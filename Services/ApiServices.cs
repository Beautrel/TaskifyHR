using System.Globalization;
using System.Net;
using System.Text.Json;
using TaskifyHR.Models;

namespace TaskifyHR.Services;

internal static class JsonExt
{
    public static string Str(this JsonElement e, string name) =>
        e.TryGetProperty(name, out var v) && v.ValueKind == JsonValueKind.String ? v.GetString() ?? "" : "";

    public static int Int(this JsonElement e, string name) =>
        e.TryGetProperty(name, out var v) && v.ValueKind == JsonValueKind.Number ? v.GetInt32() : 0;

    public static DateOnly Date(this JsonElement e, string name) =>
        DateOnly.TryParse(e.Str(name), CultureInfo.InvariantCulture, out var d) ? d : DateOnly.FromDateTime(DateTime.Today);

    public static LeaveRequest ToLeave(this JsonElement e)
    {
        var status = e.Str("status") switch
        {
            "approved" => LeaveStatus.Approved,
            "refused" => LeaveStatus.Refused,
            _ => LeaveStatus.Pending
        };
        var by = e.Str("decided_by_name");
        var refusal = e.Str("refusal_reason");
        return new LeaveRequest(
            Guid.Parse(e.Str("id")), e.Str("employee_name"), e.Str("team"),
            e.Date("start_date"), e.Date("end_date"), e.Int("days"),
            e.Str("leave_type"), e.Str("reason"), status,
            by.Length > 0 ? by : null, refusal.Length > 0 ? refusal : null);
    }

    public static UserProfile ToUser(this JsonElement e) =>
        new(e.Str("full_name"), e.Str("first_name"), e.Str("email"), e.Str("team"), e.Str("job_title"),
            e.Str("manager_name"), e.TryGetProperty("is_manager", out var m) && m.ValueKind == JsonValueKind.True,
            e.Str("employee_id"), e.Str("contract"), e.Int("weekly_hours"), e.Date("arrival_date"), e.Str("phone"));
}

public class ApiAuthService : IAuthService
{
    private readonly ApiClient _api;
    public ApiAuthService(ApiClient api) => _api = api;

    public UserProfile? CurrentUser { get; private set; }

    public async Task<bool> LoginAsync(string email, string password)
    {
        try
        {
            var r = await _api.RequestAsync(HttpMethod.Post, "auth/login/",
                new { email = email.Trim(), password }, auth: false);
            _api.SetTokens(r.Str("access"), r.Str("refresh"));
            CurrentUser = r.GetProperty("user").ToUser();
            return true;
        }
        catch (ApiException ex) when (ex.Status is HttpStatusCode.Unauthorized or HttpStatusCode.BadRequest)
        {
            return false; // identifiants incorrects
        }
    }

    public void Logout() { _api.Clear(); CurrentUser = null; }

    public async Task RegisterAsync(RegistrationRequest request) =>
        await _api.RequestAsync(HttpMethod.Post, "auth/register/", new
        {
            full_name = request.FullName.Trim(),
            email = request.Email.Trim(),
            employee_id = request.EmployeeId.Trim(),
            password = request.Password
        }, auth: false);

    public async Task UpdatePhoneAsync(string phone)
    {
        var r = await _api.RequestAsync(new HttpMethod("PATCH"), "me/", new { phone });
        CurrentUser = r.ToUser();
    }
}

public class ApiLeaveService : ILeaveService
{
    private readonly ApiClient _api;
    public ApiLeaveService(ApiClient api) => _api = api;

    public async Task<LeaveBalance> GetBalanceAsync()
    {
        var r = await _api.RequestAsync(HttpMethod.Get, "leave/balance/");
        return new LeaveBalance(r.Int("acquired"), r.Int("used"), r.Int("pending"), r.Str("period"));
    }

    public async Task<IReadOnlyList<LeaveRequest>> GetMyRequestsAsync() =>
        (await _api.RequestAsync(HttpMethod.Get, "leave/requests/")).EnumerateArray().Select(x => x.ToLeave()).ToList();

    public async Task<LeaveRequest> SubmitRequestAsync(LeaveRequest request)
    {
        // Les jours sont recalculés par le serveur (il fait foi).
        var r = await _api.RequestAsync(HttpMethod.Post, "leave/requests/", new
        {
            leave_type = request.Type,
            start_date = request.Start.ToString("yyyy-MM-dd"),
            end_date = request.End.ToString("yyyy-MM-dd"),
            reason = request.Reason
        });
        return r.ToLeave();
    }

    public async Task<IReadOnlyList<PendingApproval>> GetPendingApprovalsAsync() =>
        (await _api.RequestAsync(HttpMethod.Get, "manager/approvals/")).EnumerateArray()
            .Select(x => new PendingApproval(x.ToLeave(), x.Int("employee_remaining"), GuessFemale(x.Str("employee_name"))))
            .ToList();

    public async Task<IReadOnlyList<LeaveRequest>> GetTeamRequestsAsync() =>
        (await _api.RequestAsync(HttpMethod.Get, "manager/requests/")).EnumerateArray().Select(x => x.ToLeave()).ToList();

    public async Task<int> ApproveAsync(Guid requestId, string managerName)
    {
        var r = await _api.RequestAsync(HttpMethod.Post, $"manager/requests/{requestId}/approve/");
        return r.Int("employee_remaining");
    }

    public async Task RefuseAsync(Guid requestId, string managerName, string reason)
    {
        if (string.IsNullOrWhiteSpace(reason))
            throw new InvalidOperationException("Le motif du refus est obligatoire.");
        await _api.RequestAsync(HttpMethod.Post, $"manager/requests/{requestId}/refuse/", new { reason = reason.Trim() });
    }

    // L'API ne fournit pas le genre : heuristique simple sur le prénom (sert seulement à l'accord du texte).
    private static bool GuessFemale(string fullName)
    {
        var first = fullName.Split(' ', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault()?.ToLowerInvariant() ?? "";
        return first.EndsWith('a') || first.EndsWith('e') || first.EndsWith("ie");
    }
}

public class ApiAttendanceService : IAttendanceService
{
    private readonly ApiClient _api;
    public ApiAttendanceService(ApiClient api) => _api = api;

    public async Task<AttendanceDay> GetTodayAsync() =>
        ToDay(await _api.RequestAsync(HttpMethod.Get, "attendance/today/"));

    public async Task<AttendanceDay> ClockInAsync() => ToDay(await PostPositionAsync("attendance/clock-in/"));
    public async Task<AttendanceDay> ClockOutAsync() => ToDay(await PostPositionAsync("attendance/clock-out/"));

    private async Task<JsonElement> PostPositionAsync(string path)
    {
        var (lat, lng) = DeviceLocationService.ServerPosition;
        return await _api.RequestAsync(HttpMethod.Post, path, new { latitude = lat, longitude = lng });
    }

    private static AttendanceDay ToDay(JsonElement e)
    {
        static TimeOnly? T(string s) => TimeOnly.TryParseExact(s, "HH:mm", CultureInfo.InvariantCulture, DateTimeStyles.None, out var t) ? t : null;
        return new AttendanceDay(e.Date("date"), T(e.Str("clock_in_time")), T(e.Str("clock_out_time")));
    }
}

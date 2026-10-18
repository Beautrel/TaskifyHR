using TaskifyHR.Models;

namespace TaskifyHR.Services;

/// <summary>Connexion fictive. Identifiants de démonstration : voir les constantes ci-dessous.</summary>
public class MockAuthService : IAuthService
{
    private const string DemoEmail = "lea.martin@taskify.fr";
    private const string DemoPassword = "Taskify2026!";

    public UserProfile? CurrentUser { get; private set; }

    public async Task<bool> LoginAsync(string email, string password)
    {
        await Task.Delay(800); // simule le temps réseau (visible : état "Chargement")
        var ok = string.Equals(email.Trim(), DemoEmail, StringComparison.OrdinalIgnoreCase)
                 && password == DemoPassword;
        if (ok)
            CurrentUser = new UserProfile("Léa Martin", "Léa", DemoEmail, "Équipe Produit",
                                          "Développeuse", "Sophie Bernard");
        return ok;
    }

    public void Logout() => CurrentUser = null;
}

public class MockLeaveService : ILeaveService
{
    private const int AcquiredDays = 20;
    private const string Period = "juin 2026 – mai 2027";

    private readonly List<LeaveRequest> _requests = new()
    {
        new(Guid.NewGuid(), "Léa Martin", "Équipe Produit",
            new DateOnly(2026, 10, 19), new DateOnly(2026, 10, 21), 3,
            LeaveTypes.Paid, "Repos personnel", LeaveStatus.Pending),
        new(Guid.NewGuid(), "Léa Martin", "Équipe Produit",
            new DateOnly(2026, 8, 5), new DateOnly(2026, 8, 14), 8,
            LeaveTypes.Paid, "Vacances", LeaveStatus.Approved, ValidatedBy: "Sophie Bernard"),
    };

    /// <summary>Mettre à true (temporairement) pour tester l'état "Solde à zéro".</summary>
    public bool SimulateZeroBalance { get; set; }

    /// <summary>Mettre à true (temporairement) pour tester l'état "Liste vide".</summary>
    public bool SimulateEmptyList { get; set; }

    /// <summary>Mettre à true (temporairement) pour tester une erreur réseau (chargement ou envoi).</summary>
    public bool FailNext { get; set; }

    public async Task<LeaveBalance> GetBalanceAsync()
    {
        await Task.Delay(300);
        ThrowIfFailing();

        if (SimulateZeroBalance)
            return new LeaveBalance(AcquiredDays, AcquiredDays, 0, Period);

        // Les compteurs sont calculés à partir des demandes : ils bougent quand on envoie une demande.
        var used = _requests.Where(r => r.Status == LeaveStatus.Approved && r.Type == LeaveTypes.Paid).Sum(r => r.Days);
        var pending = _requests.Where(r => r.Status == LeaveStatus.Pending && r.Type == LeaveTypes.Paid).Sum(r => r.Days);
        return new LeaveBalance(AcquiredDays, used, pending, Period);
    }

    public async Task<IReadOnlyList<LeaveRequest>> GetMyRequestsAsync()
    {
        await Task.Delay(300);
        ThrowIfFailing();
        return SimulateEmptyList ? new List<LeaveRequest>() : _requests.ToList();
    }

    public async Task<LeaveRequest> SubmitRequestAsync(LeaveRequest request)
    {
        await Task.Delay(900);
        ThrowIfFailing();

        // Pas de doublon : même contrôle que côté serveur
        var overlap = _requests.Any(r => r.Status != LeaveStatus.Refused
                                         && r.Start <= request.End && request.Start <= r.End);
        if (overlap)
            throw new InvalidOperationException("Une demande existe déjà sur cette période.");

        _requests.Insert(0, request);
        return request;
    }

    private void ThrowIfFailing()
    {
        if (!FailNext) return;
        FailNext = false;
        throw new HttpRequestException("Réseau indisponible (simulation)");
    }
}

public class MockAttendanceService : IAttendanceService
{
    private AttendanceDay _today = new(DateOnly.FromDateTime(DateTime.Now), null, null);

    /// <summary>Mettre à true (temporairement) pour tester l'état "Erreur réseau" du pointage.</summary>
    public bool FailNext { get; set; }

    public async Task<AttendanceDay> GetTodayAsync()
    {
        await Task.Delay(400);
        return _today;
    }

    public async Task<AttendanceDay> ClockInAsync()
    {
        await Task.Delay(600);
        ThrowIfFailing();
        _today = _today with { ClockIn = TimeOnly.FromDateTime(DateTime.Now) };
        return _today;
    }

    public async Task<AttendanceDay> ClockOutAsync()
    {
        await Task.Delay(600);
        ThrowIfFailing();
        _today = _today with { ClockOut = TimeOnly.FromDateTime(DateTime.Now) };
        return _today;
    }

    private void ThrowIfFailing()
    {
        if (!FailNext) return;
        FailNext = false;
        throw new HttpRequestException("Réseau indisponible (simulation)");
    }
}

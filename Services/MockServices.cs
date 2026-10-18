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
            CurrentUser = new UserProfile("Léa Martin", "Léa", DemoEmail, "Équipe Produit", "Développeuse");
        return ok;
    }

    public void Logout() => CurrentUser = null;
}

public class MockLeaveService : ILeaveService
{
    private readonly List<LeaveRequest> _requests = new()
    {
        new(Guid.NewGuid(), "Léa Martin", "Équipe Produit",
            new DateOnly(2026, 10, 19), new DateOnly(2026, 10, 21), 3,
            "Congés payés", "Repos personnel", LeaveStatus.Pending),
        new(Guid.NewGuid(), "Léa Martin", "Équipe Produit",
            new DateOnly(2026, 8, 5), new DateOnly(2026, 8, 14), 8,
            "Congés payés", "Vacances", LeaveStatus.Approved, ValidatedBy: "Sophie Bernard"),
    };

    /// <summary>Mettre à true (temporairement) pour tester l'état "Solde à zéro".</summary>
    public bool SimulateZeroBalance { get; set; }

    /// <summary>Mettre à true (temporairement) pour tester l'état "Liste vide".</summary>
    public bool SimulateEmptyList { get; set; }

    /// <summary>Mettre à true (temporairement) pour tester l'état "Erreur de chargement".</summary>
    public bool FailNext { get; set; }

    public async Task<LeaveBalance> GetBalanceAsync()
    {
        await Task.Delay(300);
        ThrowIfFailing();
        return SimulateZeroBalance
            ? new LeaveBalance(20, 20, 0, "juin 2026 – mai 2027")
            : new LeaveBalance(20, 8, 3, "juin 2026 – mai 2027"); // mêmes chiffres que les maquettes
    }

    public async Task<IReadOnlyList<LeaveRequest>> GetMyRequestsAsync()
    {
        await Task.Delay(300);
        ThrowIfFailing();
        return SimulateEmptyList ? new List<LeaveRequest>() : _requests.ToList();
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

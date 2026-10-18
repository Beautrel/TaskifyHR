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
    public async Task<LeaveBalance> GetBalanceAsync()
    {
        await Task.Delay(300);
        return new LeaveBalance(Acquired: 20, Used: 8, Pending: 3); // mêmes chiffres que les maquettes
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

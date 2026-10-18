using TaskifyHR.Models;

namespace TaskifyHR.Services;

/// <summary>
/// Connexion fictive. Deux comptes de démonstration (même mot de passe) :
/// Léa (employée) et Sophie (manager).
/// </summary>
public class MockAuthService : IAuthService
{
    private const string DemoPassword = "Taskify2026!";

    private static readonly UserProfile[] Users =
    {
        new("Léa Martin", "Léa", "lea.martin@taskify.fr", "Équipe Produit", "Développeuse", "Sophie Bernard", false),
        new("Sophie Bernard", "Sophie", "sophie.bernard@taskify.fr", "Équipe Produit", "Responsable produit", "Direction", true),
    };

    public UserProfile? CurrentUser { get; private set; }

    public async Task<bool> LoginAsync(string email, string password)
    {
        await Task.Delay(800); // simule le temps réseau (visible : état "Chargement")
        var user = Users.FirstOrDefault(u => string.Equals(u.Email, email.Trim(), StringComparison.OrdinalIgnoreCase));
        if (user is null || password != DemoPassword)
            return false;

        CurrentUser = user;
        return true;
    }

    public void Logout() => CurrentUser = null;
}

public class MockLeaveService : ILeaveService
{
    private const int AcquiredDays = 20;
    private const string Period = "juin 2026 – mai 2027";

    private readonly IAuthService _auth;

    // Jeu de données fictif, cohérent avec les maquettes :
    // Léa : 8 j utilisés, 3 j en attente (solde 12). Hugo : 14 j utilisés, 2 j en attente (solde 6).
    private readonly List<LeaveRequest> _requests = new()
    {
        new(Guid.NewGuid(), "Léa Martin", "Équipe Produit",
            new DateOnly(2026, 10, 19), new DateOnly(2026, 10, 21), 3,
            LeaveTypes.Paid, "Repos personnel", LeaveStatus.Pending),
        new(Guid.NewGuid(), "Léa Martin", "Équipe Produit",
            new DateOnly(2026, 8, 5), new DateOnly(2026, 8, 14), 8,
            LeaveTypes.Paid, "Vacances", LeaveStatus.Approved, ValidatedBy: "Sophie Bernard"),
        new(Guid.NewGuid(), "Hugo Petit", "Équipe Produit",
            new DateOnly(2026, 10, 26), new DateOnly(2026, 10, 27), 2,
            LeaveTypes.Paid, "Rendez-vous privé", LeaveStatus.Pending),
        new(Guid.NewGuid(), "Hugo Petit", "Équipe Produit",
            new DateOnly(2026, 7, 27), new DateOnly(2026, 8, 13), 14,
            LeaveTypes.Paid, "Vacances", LeaveStatus.Approved, ValidatedBy: "Sophie Bernard"),
    };

    public MockLeaveService(IAuthService auth)
    {
        _auth = auth;
    }

    /// <summary>Mettre à true (temporairement) pour tester l'état "Solde à zéro".</summary>
    public bool SimulateZeroBalance { get; set; }

    /// <summary>Mettre à true (temporairement) pour tester l'état "Liste vide" côté employé.</summary>
    public bool SimulateEmptyList { get; set; }

    /// <summary>Mettre à true (temporairement) pour tester l'état "Liste vide" côté manager.</summary>
    public bool SimulateNoPending { get; set; }

    /// <summary>Mettre à true (temporairement) pour tester une erreur réseau (chargement, envoi, décision).</summary>
    public bool FailNext { get; set; }

    private string CurrentName => _auth.CurrentUser?.FullName ?? "";

    // ----- Côté employé -----

    public async Task<LeaveBalance> GetBalanceAsync()
    {
        await Task.Delay(300);
        ThrowIfFailing();

        if (SimulateZeroBalance)
            return new LeaveBalance(AcquiredDays, AcquiredDays, 0, Period);

        return BalanceOf(CurrentName);
    }

    public async Task<IReadOnlyList<LeaveRequest>> GetMyRequestsAsync()
    {
        await Task.Delay(300);
        ThrowIfFailing();
        if (SimulateEmptyList)
            return new List<LeaveRequest>();

        return _requests.Where(r => r.EmployeeName == CurrentName).ToList();
    }

    public async Task<LeaveRequest> SubmitRequestAsync(LeaveRequest request)
    {
        await Task.Delay(900);
        ThrowIfFailing();

        // Pas de doublon : même contrôle que côté serveur
        var overlap = _requests.Any(r => r.EmployeeName == request.EmployeeName
                                         && r.Status != LeaveStatus.Refused
                                         && r.Start <= request.End && request.Start <= r.End);
        if (overlap)
            throw new InvalidOperationException("Une demande existe déjà sur cette période.");

        _requests.Insert(0, request);
        return request;
    }

    // ----- Côté manager -----

    public async Task<IReadOnlyList<PendingApproval>> GetPendingApprovalsAsync()
    {
        await Task.Delay(400);
        ThrowIfFailing();
        if (SimulateNoPending)
            return new List<PendingApproval>();

        return _requests
            .Where(r => r.Status == LeaveStatus.Pending && r.EmployeeName != CurrentName)
            .OrderBy(r => r.Start)
            .Select(r => new PendingApproval(r, BalanceOf(r.EmployeeName).Remaining, IsFemale(r.EmployeeName)))
            .ToList();
    }

    public async Task<int> ApproveAsync(Guid requestId, string managerName)
    {
        await Task.Delay(700);
        ThrowIfFailing();

        var index = FindPending(requestId);
        var request = _requests[index];

        // Règle : vérifier le solde au moment de la décision
        if (request.Type == LeaveTypes.Paid && request.Days > BalanceOf(request.EmployeeName).Remaining)
            throw new InvalidOperationException("Solde insuffisant : la demande ne peut plus être approuvée.");

        _requests[index] = request with { Status = LeaveStatus.Approved, ValidatedBy = managerName };
        return BalanceOf(request.EmployeeName).Remaining; // les jours sont déduits uniquement maintenant
    }

    public async Task RefuseAsync(Guid requestId, string managerName, string reason)
    {
        await Task.Delay(700);
        ThrowIfFailing();

        // Règle : un refus sans motif est impossible
        if (string.IsNullOrWhiteSpace(reason))
            throw new InvalidOperationException("Le motif du refus est obligatoire.");

        var index = FindPending(requestId);
        _requests[index] = _requests[index] with
        {
            Status = LeaveStatus.Refused,
            ValidatedBy = managerName,
            RefusalReason = reason.Trim()
        };
        // Aucun jour déduit : le solde est calculé à partir des demandes approuvées seulement.
    }

    // ----- Utilitaires -----

    private int FindPending(Guid requestId)
    {
        var index = _requests.FindIndex(r => r.Id == requestId);
        if (index < 0)
            throw new InvalidOperationException("Demande introuvable.");
        if (_requests[index].Status != LeaveStatus.Pending)
            throw new InvalidOperationException("Cette demande a déjà été traitée.");
        return index;
    }

    // Les compteurs sont calculés à partir des demandes : ils bougent à chaque décision.
    private LeaveBalance BalanceOf(string employee)
    {
        var paid = _requests.Where(r => r.EmployeeName == employee && r.Type == LeaveTypes.Paid).ToList();
        var used = paid.Where(r => r.Status == LeaveStatus.Approved).Sum(r => r.Days);
        var pending = paid.Where(r => r.Status == LeaveStatus.Pending).Sum(r => r.Days);
        return new LeaveBalance(AcquiredDays, used, pending, Period);
    }

    private static bool IsFemale(string name) => name is "Léa Martin" or "Sophie Bernard";

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

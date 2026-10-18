using TaskifyHR.Models;

namespace TaskifyHR.Services;

// Les ViewModels dépendent de ces interfaces, jamais des classes "mock" :
// pour brancher une vraie API, on remplacera juste l'implémentation dans MauiProgram.cs.

public interface IAuthService
{
    UserProfile? CurrentUser { get; }
    Task<bool> LoginAsync(string email, string password);
    void Logout();

    /// <summary>Crée un compte. Lève InvalidOperationException si l'e-mail ou le matricule existe déjà.</summary>
    Task RegisterAsync(RegistrationRequest request);

    /// <summary>Met à jour le téléphone de l'utilisateur connecté (seule donnée modifiable du profil).</summary>
    Task UpdatePhoneAsync(string phone);
}

public interface ILeaveService
{
    // --- Côté employé ---
    Task<LeaveBalance> GetBalanceAsync();
    Task<IReadOnlyList<LeaveRequest>> GetMyRequestsAsync();

    /// <summary>Enregistre une nouvelle demande (statut En attente). Lève InvalidOperationException en cas de doublon.</summary>
    Task<LeaveRequest> SubmitRequestAsync(LeaveRequest request);

    // --- Côté manager ---
    Task<IReadOnlyList<PendingApproval>> GetPendingApprovalsAsync();

    /// <summary>Approuve une demande (le solde est vérifié à la décision). Retourne le nouveau solde de l'employé.</summary>
    Task<int> ApproveAsync(Guid requestId, string managerName);

    /// <summary>Refuse une demande. Le motif est obligatoire ; les jours ne sont pas déduits.</summary>
    Task RefuseAsync(Guid requestId, string managerName, string reason);
}

public interface IAttendanceService
{
    Task<AttendanceDay> GetTodayAsync();
    Task<AttendanceDay> ClockInAsync();
    Task<AttendanceDay> ClockOutAsync();
}

public enum LocationCheck { InZone, OutOfZone, PermissionDenied, Unavailable }

public interface ILocationService
{
    /// <summary>Demande la position une seule fois (au clic de pointage) et vérifie la zone.</summary>
    Task<LocationCheck> CheckAsync();
}

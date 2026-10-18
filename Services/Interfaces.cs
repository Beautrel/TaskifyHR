using TaskifyHR.Models;

namespace TaskifyHR.Services;

// Les ViewModels dépendent de ces interfaces, jamais des classes "mock" :
// pour brancher une vraie API, on remplacera juste l'implémentation dans MauiProgram.cs.

public interface IAuthService
{
    UserProfile? CurrentUser { get; }
    Task<bool> LoginAsync(string email, string password);
    void Logout();
}

public interface ILeaveService
{
    Task<LeaveBalance> GetBalanceAsync();
    Task<IReadOnlyList<LeaveRequest>> GetMyRequestsAsync();

    /// <summary>Enregistre une nouvelle demande (statut En attente). Lève InvalidOperationException en cas de doublon.</summary>
    Task<LeaveRequest> SubmitRequestAsync(LeaveRequest request);
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

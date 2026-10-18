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

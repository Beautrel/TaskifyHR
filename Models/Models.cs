namespace TaskifyHR.Models;

/// <summary>Utilisateur connecté.</summary>
public record UserProfile(string FullName, string FirstName, string Email, string Team, string Role);

/// <summary>Où en est la journée de pointage.</summary>
public enum ClockStatus { BeforeEntry, InProgress, Done }

/// <summary>Pointage du jour : heure d'entrée et de sortie (null = pas encore pointé).</summary>
public record AttendanceDay(DateOnly Date, TimeOnly? ClockIn, TimeOnly? ClockOut);

/// <summary>Compteurs de congés payés.</summary>
public record LeaveBalance(int Acquired, int Used, int Pending)
{
    public int Remaining => Acquired - Used;
}

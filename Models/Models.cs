namespace TaskifyHR.Models;

/// <summary>Utilisateur connecté.</summary>
public record UserProfile(string FullName, string FirstName, string Email, string Team, string Role,
                          string ManagerName, bool IsManager,
                          string EmployeeId, string Contract, int WeeklyHours, DateOnly Arrival, string Phone);

/// <summary>Où en est la journée de pointage.</summary>
public enum ClockStatus { BeforeEntry, InProgress, Done }

/// <summary>Pointage du jour : heure d'entrée et de sortie (null = pas encore pointé).</summary>
public record AttendanceDay(DateOnly Date, TimeOnly? ClockIn, TimeOnly? ClockOut);

/// <summary>Compteurs de congés payés.</summary>
public record LeaveBalance(int Acquired, int Used, int Pending, string Period)
{
    public int Remaining => Acquired - Used;
}

/// <summary>Statut d'une demande de congé.</summary>
public enum LeaveStatus { Pending, Approved, Refused }

/// <summary>Types de congé. Seuls les congés payés consomment le solde.</summary>
public static class LeaveTypes
{
    public const string Paid = "Congés payés";
    public const string Unpaid = "Congé sans solde";
    public static readonly string[] All = { Paid, Unpaid };
}

/// <summary>Une demande de congé.</summary>
public record LeaveRequest(
    Guid Id,
    string EmployeeName,
    string Team,
    DateOnly Start,
    DateOnly End,
    int Days,
    string Type,
    string Reason,
    LeaveStatus Status,
    string? ValidatedBy = null,
    string? RefusalReason = null);

/// <summary>Demande en attente de décision, avec le solde actuel de l'employé (vu par le manager).</summary>
public record PendingApproval(LeaveRequest Request, int EmployeeRemaining, bool EmployeeIsFemale);

/// <summary>Données saisies sur l'écran d'inscription.</summary>
public record RegistrationRequest(string FullName, string Email, string EmployeeId, string Password);

/// <summary>Message envoyé à l'écran de connexion après une inscription réussie (pour préremplir l'e-mail).</summary>
public record AccountCreatedMessage(string Email);

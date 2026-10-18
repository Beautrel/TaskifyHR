using System.Collections.ObjectModel;
using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using TaskifyHR.Helpers;
using TaskifyHR.Models;
using TaskifyHR.Services;

namespace TaskifyHR.ViewModels;

/// <summary>Une case du calendrier. Kind : "None", "Pending" (orange) ou "Approved" (vert).</summary>
public record DayCell(int Number, int Row, int Column, string Kind, bool IsToday, bool IsWeekend);

/// <summary>Une carte sous le calendrier (aujourd'hui, ou un congé du mois).</summary>
public record PlanningEvent(string Title, string Detail, string Note, LeaveStatus Status, bool HasBadge)
{
    public bool HasNote => !string.IsNullOrEmpty(Note);
}

public partial class PlanningViewModel : ObservableObject
{
    private static readonly CultureInfo Fr = new("fr-FR");

    private readonly ILeaveService _leave;
    private readonly IAttendanceService _attendance;
    private readonly IAuthService _auth;

    private DateTime _month = FirstOfMonth(DateTime.Today);
    private IReadOnlyList<LeaveRequest> _requests = Array.Empty<LeaveRequest>();
    private AttendanceDay? _today;

    public PlanningViewModel(ILeaveService leave, IAttendanceService attendance, IAuthService auth)
    {
        _leave = leave;
        _attendance = attendance;
        _auth = auth;
        MonthTitle = MonthLabel(_month);
    }

    public ObservableCollection<DayCell> Days { get; } = new();
    public ObservableCollection<PlanningEvent> Events { get; } = new();

    [ObservableProperty] private string monthTitle = "";
    [ObservableProperty] private string loadingMessage = "";
    [ObservableProperty] private bool isLoading;
    [ObservableProperty] private bool hasError;
    [ObservableProperty] private bool isEmpty;
    [ObservableProperty] private bool showCalendar = true;

    [RelayCommand]
    private async Task LoadAsync()
    {
        HasError = false;
        IsEmpty = false;
        LoadingMessage = $"Chargement du planning {OfMonth(_month)}…";
        IsLoading = true;
        try
        {
            _requests = await _leave.GetMyRequestsAsync();
            _today = await _attendance.GetTodayAsync();
            ShowCalendar = true;
            Build();
        }
        catch (Exception)
        {
            Days.Clear();
            Events.Clear();
            ShowCalendar = false;
            HasError = true;
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private void PreviousMonth()
    {
        _month = _month.AddMonths(-1);
        Build();
    }

    [RelayCommand]
    private void NextMonth()
    {
        _month = _month.AddMonths(1);
        Build();
    }

    [RelayCommand]
    private void BackToCurrentMonth()
    {
        _month = FirstOfMonth(DateTime.Today);
        Build();
    }

    /// <summary>Reconstruit le calendrier et la liste pour le mois affiché.</summary>
    private void Build()
    {
        MonthTitle = MonthLabel(_month);
        var today = DateTime.Today;
        var daysInMonth = DateTime.DaysInMonth(_month.Year, _month.Month);
        var offset = ColumnOf(_month); // colonne du 1er du mois (lundi = 0)

        // Congés visibles : on ignore les demandes refusées
        var visible = _requests.Where(r => r.Status != LeaveStatus.Refused).ToList();

        Days.Clear();
        for (var d = 1; d <= daysInMonth; d++)
        {
            var date = new DateTime(_month.Year, _month.Month, d);
            var dateOnly = DateOnly.FromDateTime(date);
            var weekend = date.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday;

            // Seuls les jours ouvrés sont colorés (les week-ends ne sont pas décomptés)
            var covering = weekend
                ? new List<LeaveRequest>()
                : visible.Where(r => r.Start <= dateOnly && dateOnly <= r.End).ToList();
            var kind = covering.Any(r => r.Status == LeaveStatus.Approved) ? "Approved"
                     : covering.Any() ? "Pending"
                     : "None";

            Days.Add(new DayCell(d, (offset + d - 1) / 7, ColumnOf(date), kind, date.Date == today, weekend));
        }

        Events.Clear();
        if (today.Year == _month.Year && today.Month == _month.Month)
            Events.Add(TodayEvent(today));

        var first = DateOnly.FromDateTime(_month);
        var last = DateOnly.FromDateTime(_month.AddMonths(1).AddDays(-1));
        foreach (var r in visible.Where(r => r.Start <= last && first <= r.End).OrderBy(r => r.Start))
        {
            var note = r.Status == LeaveStatus.Approved && !string.IsNullOrEmpty(r.ValidatedBy)
                ? $"Validés par {r.ValidatedBy}."
                : "";
            Events.Add(new PlanningEvent(
                FrFormat.DateRange(r.Start, r.End),
                $"{r.Type} · {r.Days} jours ouvrés",
                note, r.Status, HasBadge: true));
        }

        IsEmpty = Events.Count == 0;
    }

    private PlanningEvent TodayEvent(DateTime today)
    {
        string detail;
        if (today.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday)
        {
            detail = "Week-end · Aucun horaire prévu";
        }
        else if (_today is { ClockIn: { } clockIn, ClockOut: null })
        {
            detail = $"Sur site · Entrée à {clockIn:HH:mm}";
        }
        else
        {
            detail = "Horaire prévu : 09:00 – 17:00";
        }

        return new PlanningEvent($"Aujourd'hui · {FrFormat.ShortDate(today)}", detail,
                                 _auth.CurrentUser?.Team ?? "", LeaveStatus.Pending, HasBadge: false);
    }

    // ----- Utilitaires -----

    private static DateTime FirstOfMonth(DateTime date) => new(date.Year, date.Month, 1);

    // Lundi = 0 ... Dimanche = 6 (le calendrier français commence le lundi)
    private static int ColumnOf(DateTime date) => ((int)date.DayOfWeek + 6) % 7;

    private static string MonthLabel(DateTime month)
    {
        var s = month.ToString("MMMM yyyy", Fr);
        return char.ToUpper(s[0]) + s[1..];
    }

    // "du planning d'octobre 2026" / "du planning de novembre 2026"
    private static string OfMonth(DateTime month)
    {
        var name = month.ToString("MMMM", Fr);
        var elide = "aeiouhéèêô".Contains(name[0]);
        return $"{(elide ? "d'" : "de ")}{name} {month.Year}";
    }
}

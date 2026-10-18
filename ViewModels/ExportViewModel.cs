using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using TaskifyHR.Helpers;
using TaskifyHR.Models;
using TaskifyHR.Services;

namespace TaskifyHR.ViewModels;

public partial class ExportViewModel : ObservableObject
{
    private static readonly CultureInfo Fr = new("fr-FR");

    private readonly ILeaveService _leave;
    private readonly IAuthService _auth;
    private readonly IExportService _export;

    private DateTime _month = new(DateTime.Today.Year, DateTime.Today.Month, 1);
    private IReadOnlyList<LeaveRequest> _requests = Array.Empty<LeaveRequest>();
    private List<ReportLine> _lines = new();

    public ExportViewModel(ILeaveService leave, IAuthService auth, IExportService export)
    {
        _leave = leave;
        _auth = auth;
        _export = export;
    }

    public string[] Formats { get; } = { "PDF", "CSV" };

    [ObservableProperty] private string selectedFormat = "PDF";
    [ObservableProperty] private string monthTitle = "";
    [ObservableProperty] private string heroSubtitle = "";
    [ObservableProperty] private string summaryTitle = "";
    [ObservableProperty] private string summaryDetail = "";

    [ObservableProperty] private bool isLoading;
    [ObservableProperty] private bool hasError;
    [ObservableProperty] private bool isDenied;
    [ObservableProperty] private bool isEmpty;
    [ObservableProperty] private bool showContent;

    [ObservableProperty, NotifyCanExecuteChangedFor(nameof(ExportCommand))]
    private bool canExport;

    [ObservableProperty, NotifyPropertyChangedFor(nameof(ExportText))]
    private bool isBusy;

    [ObservableProperty, NotifyPropertyChangedFor(nameof(HasExportError))]
    private string exportError = "";

    [ObservableProperty] private bool hasResult;
    [ObservableProperty] private string resultMessage = "";

    public bool HasExportError => !string.IsNullOrEmpty(ExportError);
    public string ExportText => IsBusy ? "Chargement…" : "Exporter";

    partial void OnIsBusyChanged(bool value) => UpdateCanExport();

    [RelayCommand]
    private async Task LoadAsync()
    {
        HasError = false;
        IsEmpty = false;
        ShowContent = false;

        // Règle : export réservé au manager
        var user = _auth.CurrentUser;
        if (user is not { IsManager: true })
        {
            IsDenied = true;
            return;
        }
        IsDenied = false;
        HeroSubtitle = $"{user.FullName} · {user.Team}";

        IsLoading = true;
        try
        {
            _requests = await _leave.GetTeamRequestsAsync();
            ShowContent = true;
            Build();
        }
        catch (Exception)
        {
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

    /// <summary>Calcule l'aperçu du mois choisi : une ligne par demande qui touche ce mois.</summary>
    private void Build()
    {
        HasResult = false;
        ExportError = "";

        var name = _month.ToString("MMMM yyyy", Fr);
        MonthTitle = char.ToUpper(name[0]) + name[1..];

        var first = DateOnly.FromDateTime(_month);
        var last = DateOnly.FromDateTime(_month.AddMonths(1).AddDays(-1));

        _lines = _requests
            .Where(r => r.Start <= last && first <= r.End)
            .OrderBy(r => r.EmployeeName).ThenBy(r => r.Start)
            .Select(r => new ReportLine(
                r.EmployeeName, r.Type, r.Start, r.End,
                // seuls les jours ouvrés qui tombent dans ce mois sont comptés
                WorkingDays.Count(r.Start < first ? first : r.Start, r.End > last ? last : r.End),
                r.Status))
            .ToList();

        var report = ToReport();
        var plural = _lines.Count > 1 ? "s" : "";
        SummaryTitle = $"{_lines.Count} demande{plural} en {MonthTitle.ToLower(Fr)}";
        SummaryDetail = $"Jours approuvés : {report.ApprovedDays} · En attente : {report.PendingDays} · Refusées : {report.RefusedCount}";

        IsEmpty = _lines.Count == 0;
        UpdateCanExport();
    }

    private MonthlyReport ToReport()
    {
        var user = _auth.CurrentUser;
        return new MonthlyReport(_month, user?.FullName ?? "", user?.Team ?? "", _lines);
    }

    private void UpdateCanExport() => CanExport = ShowContent && !IsEmpty && !IsBusy;

    [RelayCommand(CanExecute = nameof(CanExport))]
    private async Task ExportAsync()
    {
        ExportError = "";
        HasResult = false;
        IsBusy = true;
        try
        {
            var format = SelectedFormat == "CSV" ? ExportFormat.Csv : ExportFormat.Pdf;
            var path = await _export.CreateFileAsync(ToReport(), format);

            ResultMessage = $"Fichier généré : {Path.GetFileName(path)}";
            HasResult = true;

            await _export.ShareAsync(path); // feuille de partage Android
        }
        catch (Exception)
        {
            HasResult = false;
            ExportError = "Export impossible. Réessayez.";
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task BackAsync() => await Shell.Current.GoToAsync("..");
}

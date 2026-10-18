using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using TaskifyHR.Helpers;
using TaskifyHR.Models;
using TaskifyHR.Services;

namespace TaskifyHR.ViewModels;

public partial class LeaveRequestViewModel : ObservableObject
{
    private readonly ILeaveService _leave;
    private readonly IAuthService _auth;

    private int _remaining;
    private IReadOnlyList<LeaveRequest> _existing = Array.Empty<LeaveRequest>();
    private bool _loaded;
    private bool _valid;

    public LeaveRequestViewModel(ILeaveService leave, IAuthService auth)
    {
        _leave = leave;
        _auth = auth;
    }

    /// <summary>Demandé par la page : remonter en haut de l'écran (bouton "Modifier les dates").</summary>
    public event Action? ScrollToTopRequested;

    public string[] Types => LeaveTypes.All;

    // ----- Champs du formulaire -----
    [ObservableProperty] private string leaveType = LeaveTypes.Paid;
    [ObservableProperty] private DateTime startDate = DateTime.Today.AddDays(1);
    [ObservableProperty] private DateTime endDate = DateTime.Today.AddDays(1);
    [ObservableProperty] private string reason = "";

    // ----- Résultats de validation -----
    [ObservableProperty, NotifyPropertyChangedFor(nameof(HasStartError))] private string startError = "";
    [ObservableProperty, NotifyPropertyChangedFor(nameof(HasEndError))] private string endError = "";
    [ObservableProperty, NotifyPropertyChangedFor(nameof(HasFormError))] private string formError = "";
    [ObservableProperty] private bool hasDateError;
    [ObservableProperty] private bool isInsufficient;
    [ObservableProperty] private string insufficientMessage = "";
    [ObservableProperty] private string summaryTitle = "";
    [ObservableProperty] private string summaryDetail = "";
    [ObservableProperty] private string managerText = "";
    [ObservableProperty] private int days;

    [ObservableProperty, NotifyCanExecuteChangedFor(nameof(SubmitCommand))]
    private bool canSubmit;

    // ----- États de l'écran -----
    [ObservableProperty, NotifyPropertyChangedFor(nameof(SubmitText))]
    private bool isSending;

    [ObservableProperty, NotifyPropertyChangedFor(nameof(IsFormVisible))]
    private bool isSent;

    [ObservableProperty] private string sendingMessage = "";
    [ObservableProperty] private string sentMessage = "";

    public bool HasStartError => !string.IsNullOrEmpty(StartError);
    public bool HasEndError => !string.IsNullOrEmpty(EndError);
    public bool HasFormError => !string.IsNullOrEmpty(FormError);
    public bool IsFormVisible => !IsSent;
    public string SubmitText => IsSending ? "Chargement…" : "Envoyer la demande";

    // Dès qu'un champ change, on revalide tout (ces méthodes sont appelées automatiquement).
    partial void OnStartDateChanged(DateTime value) => Recompute();
    partial void OnEndDateChanged(DateTime value) => Recompute();
    partial void OnLeaveTypeChanged(string value) => Recompute();
    partial void OnIsSendingChanged(bool value) => CanSubmit = _valid && !value;

    [RelayCommand]
    private async Task LoadAsync()
    {
        try
        {
            var balance = await _leave.GetBalanceAsync();
            _existing = await _leave.GetMyRequestsAsync();
            _remaining = balance.Remaining;
        }
        catch (Exception)
        {
            _remaining = 0;
            _existing = Array.Empty<LeaveRequest>();
        }

        ManagerText = $"La demande sera transmise à {_auth.CurrentUser?.ManagerName ?? "votre manager"}.";

        // Valeurs par défaut : prochain jour ouvré
        var next = DateTime.Today.AddDays(1);
        while (next.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday)
            next = next.AddDays(1);

        _loaded = true;
        IsSent = false;
        Reason = "";
        StartDate = next;
        EndDate = next;
        Recompute();
    }

    /// <summary>Applique les règles métier de l'énoncé et met à jour tous les messages.</summary>
    private void Recompute()
    {
        if (!_loaded) return;

        var start = DateOnly.FromDateTime(StartDate);
        var end = DateOnly.FromDateTime(EndDate);

        // Règle : dates futures, début <= fin
        StartError = StartDate.Date <= DateTime.Today ? "La date de début doit être future." : "";
        EndError = EndDate.Date < StartDate.Date ? "La fin doit être après le début." : "";
        HasDateError = HasStartError || HasEndError;

        // Règle : jours ouvrés du lundi au vendredi
        Days = HasDateError ? 0 : WorkingDays.Count(start, end);

        // Règle : le solde ne doit pas être dépassé (congés payés uniquement)
        var usesBalance = LeaveType == LeaveTypes.Paid;
        IsInsufficient = !HasDateError && usesBalance && Days > _remaining;
        InsufficientMessage = IsInsufficient
            ? $"{Days} jours demandés du {FrFormat.ShortDate(StartDate)} au {FrFormat.ShortDate(EndDate)}. " +
              $"Votre solde est de {_remaining} jours. Réduisez la durée ou changez de type."
            : "";

        // Messages généraux
        FormError = "";
        if (!HasDateError && Days == 0)
            FormError = "Cette période ne contient aucun jour ouvré (lundi à vendredi).";
        else if (!HasDateError && _existing.Any(r => r.Status != LeaveStatus.Refused
                                                     && r.Start <= end && start <= r.End))
            FormError = "Une demande existe déjà sur cette période.";

        // Carte récapitulative
        SummaryTitle = Days == 1 ? "1 jour ouvré" : $"{Days} jours ouvrés";
        SummaryDetail = !usesBalance
            ? "Sans impact sur votre solde. Week-ends exclus du calcul."
            : IsInsufficient || HasDateError
                ? $"Solde actuel : {_remaining} j. Week-ends exclus du calcul."
                : $"Solde actuel : {_remaining} j · Après accord : {_remaining - Days} j. Week-ends exclus du calcul.";

        _valid = !HasDateError && Days > 0 && !IsInsufficient && !HasFormError;
        CanSubmit = _valid && !IsSending;
    }

    [RelayCommand(CanExecute = nameof(CanSubmit))]
    private async Task SubmitAsync()
    {
        var start = DateOnly.FromDateTime(StartDate);
        var end = DateOnly.FromDateTime(EndDate);
        var range = $"{FrFormat.DateRange(start, end)} · {Days} jour{(Days > 1 ? "s" : "")}";

        SendingMessage = $"Envoi de votre demande…\n{range}";
        FormError = "";
        IsSending = true; // évite aussi l'envoi en double (bouton désactivé)
        try
        {
            var user = _auth.CurrentUser;
            var request = new LeaveRequest(Guid.NewGuid(), user?.FullName ?? "", user?.Team ?? "",
                                           start, end, Days, LeaveType, (Reason ?? "").Trim(),
                                           LeaveStatus.Pending);
            await _leave.SubmitRequestAsync(request);

            SentMessage = $"Votre demande a été transmise à {user?.ManagerName ?? "votre manager"}.\n{range}";
            IsSent = true;
        }
        catch (InvalidOperationException)
        {
            FormError = "Une demande existe déjà sur cette période.";
        }
        catch (Exception)
        {
            FormError = "Envoi impossible. Vérifiez votre connexion et réessayez.";
        }
        finally
        {
            IsSending = false;
        }
    }

    [RelayCommand]
    private void EditDates() => ScrollToTopRequested?.Invoke();

    [RelayCommand]
    private async Task BackAsync() => await Shell.Current.GoToAsync("..");

    [RelayCommand]
    private async Task ViewMyRequestsAsync() => await Shell.Current.GoToAsync("//main/balance");
}

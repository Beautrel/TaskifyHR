using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using TaskifyHR.Helpers;
using TaskifyHR.Models;
using TaskifyHR.Services;

namespace TaskifyHR.ViewModels;

/// <summary>Où en est le traitement d'une demande dans la liste du manager.</summary>
public enum ApprovalPhase { Pending, Refusing, Approved, Refused }

/// <summary>
/// Une demande dans l'écran manager. Chaque carte a sa propre petite machine à états :
/// En attente → (Approuvée) ou (Refus en cours → Refusée).
/// </summary>
public partial class ApprovalItem : ObservableObject
{
    private readonly PendingApproval _source;
    private readonly ILeaveService _leave;
    private readonly string _managerName;
    private readonly Action _onChanged;

    public ApprovalItem(PendingApproval source, ILeaveService leave, string managerName, Action onChanged)
    {
        _source = source;
        _leave = leave;
        _managerName = managerName;
        _onChanged = onChanged;

        var r = source.Request;
        var plural = r.Days > 1 ? "s" : "";
        EmployeeName = r.EmployeeName;
        DateAndDays = $"{FrFormat.DateRange(r.Start, r.End)} · {r.Days} jour{plural}";
        TypeAndReason = string.IsNullOrWhiteSpace(r.Reason) ? r.Type : $"{r.Type} · {r.Reason}";
        BalanceLine = $"Solde disponible : {source.EmployeeRemaining} jours";
        RefusalSummary = $"{DateAndDays}\nSolde : {source.EmployeeRemaining} jours";
    }

    public string EmployeeName { get; }
    public string DateAndDays { get; }
    public string TypeAndReason { get; }
    public string BalanceLine { get; }
    public string RefusalSummary { get; }

    private string FirstName => EmployeeName.Split(' ')[0];
    private string Informed => _source.EmployeeIsFemale ? "informée" : "informé";

    [ObservableProperty,
     NotifyPropertyChangedFor(nameof(IsPending)),
     NotifyPropertyChangedFor(nameof(IsRefusing)),
     NotifyPropertyChangedFor(nameof(IsApproved)),
     NotifyPropertyChangedFor(nameof(IsRefused)),
     NotifyPropertyChangedFor(nameof(IsOpen))]
    private ApprovalPhase phase = ApprovalPhase.Pending;

    [ObservableProperty,
     NotifyPropertyChangedFor(nameof(ApproveText)),
     NotifyPropertyChangedFor(nameof(ConfirmText)),
     NotifyCanExecuteChangedFor(nameof(ApproveCommand)),
     NotifyCanExecuteChangedFor(nameof(StartRefusalCommand)),
     NotifyCanExecuteChangedFor(nameof(ConfirmRefusalCommand)),
     NotifyCanExecuteChangedFor(nameof(CancelRefusalCommand))]
    private bool isBusy;

    [ObservableProperty,
     NotifyPropertyChangedFor(nameof(HasReasonError)),
     NotifyCanExecuteChangedFor(nameof(ConfirmRefusalCommand))]
    private string refusalReason = "";

    [ObservableProperty, NotifyPropertyChangedFor(nameof(HasError))]
    private string errorMessage = "";

    [ObservableProperty] private string resultMessage = "";

    public bool IsPending => Phase == ApprovalPhase.Pending;
    public bool IsRefusing => Phase == ApprovalPhase.Refusing;
    public bool IsApproved => Phase == ApprovalPhase.Approved;
    public bool IsRefused => Phase == ApprovalPhase.Refused;
    public bool IsOpen => Phase is ApprovalPhase.Pending or ApprovalPhase.Refusing;

    public bool HasError => !string.IsNullOrEmpty(ErrorMessage);
    public bool HasReasonError => string.IsNullOrWhiteSpace(RefusalReason);
    public string ApproveText => IsBusy ? "Chargement…" : "Approuver";
    public string ConfirmText => IsBusy ? "Chargement…" : "Confirmer le refus";

    [RelayCommand(CanExecute = nameof(CanAct))]
    private async Task ApproveAsync()
    {
        ErrorMessage = "";
        IsBusy = true;
        try
        {
            var newBalance = await _leave.ApproveAsync(_source.Request.Id, _managerName);
            ResultMessage = $"La demande de {EmployeeName} est approuvée.\n{DateAndDays}\n" +
                            $"Nouveau solde : {newBalance} jours. {FirstName} a été {Informed}.";
            Phase = ApprovalPhase.Approved;
            _onChanged();
        }
        catch (InvalidOperationException ex) // règle métier refusée (solde insuffisant, déjà traitée...)
        {
            ErrorMessage = ex.Message;
        }
        catch (Exception)
        {
            ErrorMessage = "Action impossible. Vérifiez votre connexion et réessayez.";
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand(CanExecute = nameof(CanAct))]
    private void StartRefusal()
    {
        ErrorMessage = "";
        RefusalReason = "";
        Phase = ApprovalPhase.Refusing;
    }

    [RelayCommand(CanExecute = nameof(CanAct))]
    private void CancelRefusal()
    {
        ErrorMessage = "";
        Phase = ApprovalPhase.Pending;
    }

    // Règle : un refus sans motif est impossible (le bouton reste désactivé tant que le motif est vide).
    [RelayCommand(CanExecute = nameof(CanConfirmRefusal))]
    private async Task ConfirmRefusalAsync()
    {
        ErrorMessage = "";
        IsBusy = true;
        try
        {
            var reason = RefusalReason.Trim();
            await _leave.RefuseAsync(_source.Request.Id, _managerName, reason);

            var shown = reason.TrimEnd('.');
            shown = char.ToLower(shown[0]) + shown[1..];
            ResultMessage = $"La demande de {EmployeeName} est refusée.\nMotif : {shown}.\n" +
                            $"Solde inchangé : {_source.EmployeeRemaining} jours. {FirstName} a été {Informed}.";
            Phase = ApprovalPhase.Refused;
            _onChanged();
        }
        catch (InvalidOperationException ex)
        {
            ErrorMessage = ex.Message;
        }
        catch (Exception)
        {
            ErrorMessage = "Action impossible. Vérifiez votre connexion et réessayez.";
        }
        finally
        {
            IsBusy = false;
        }
    }

    private bool CanAct() => !IsBusy;
    private bool CanConfirmRefusal() => !IsBusy && !string.IsNullOrWhiteSpace(RefusalReason);
}

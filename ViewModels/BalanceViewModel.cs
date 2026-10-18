using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using TaskifyHR.Helpers;
using TaskifyHR.Models;
using TaskifyHR.Services;

namespace TaskifyHR.ViewModels;

/// <summary>Ce que la liste "Mes demandes" affiche pour une demande.</summary>
public record LeaveRequestItem(string DateRange, string Detail, LeaveStatus Status, string Note);

public partial class BalanceViewModel : ObservableObject
{
    private readonly ILeaveService _leave;

    public BalanceViewModel(ILeaveService leave)
    {
        _leave = leave;
    }

    public ObservableCollection<LeaveRequestItem> Requests { get; } = new();

    [ObservableProperty] private string heroTitle = "";
    [ObservableProperty] private string heroSubtitle = "";
    [ObservableProperty] private string summaryTitle = "";
    [ObservableProperty] private string periodText = "";
    [ObservableProperty] private bool isLoading;
    [ObservableProperty] private bool hasError;
    [ObservableProperty] private bool isEmpty;
    [ObservableProperty] private bool isZero;

    [ObservableProperty, NotifyCanExecuteChangedFor(nameof(RequestLeaveCommand))]
    private bool canRequest = true;

    [RelayCommand]
    private async Task LoadAsync()
    {
        HasError = false;
        IsLoading = true;
        try
        {
            var balance = await _leave.GetBalanceAsync();
            var requests = await _leave.GetMyRequestsAsync();

            HeroTitle = $"{balance.Remaining} jours restants";
            HeroSubtitle = $"Congés payés · au {FrFormat.LongDate(DateTime.Now)}";
            SummaryTitle = $"{balance.Acquired} j acquis · {balance.Used} j utilisés";
            PeriodText = $"Période : {balance.Period}";

            IsZero = balance.Remaining <= 0;
            CanRequest = !IsZero;

            Requests.Clear();
            foreach (var r in requests)
                Requests.Add(ToItem(r));
            IsEmpty = Requests.Count == 0;
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

    [RelayCommand(CanExecute = nameof(CanRequest))]
    private async Task RequestLeaveAsync() =>
        await Shell.Current.GoToAsync("leave-request");

    private static LeaveRequestItem ToItem(LeaveRequest r) => new(
        DateRange: FrFormat.DateRange(r.Start, r.End),
        Detail: r.Status == LeaveStatus.Approved
            ? $"{r.Days} jours utilisés"
            : $"{r.Days} jours · {r.Type}",
        Status: r.Status,
        Note: r.Status switch
        {
            LeaveStatus.Pending => $"Ces {r.Days} jours ne sont pas encore déduits.",
            LeaveStatus.Approved => $"Validés par {r.ValidatedBy}.",
            _ => $"Refusé : {r.RefusalReason}"
        });
}

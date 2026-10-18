using System.Text.RegularExpressions;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using TaskifyHR.Helpers;
using TaskifyHR.Services;

namespace TaskifyHR.ViewModels;

public partial class ProfileViewModel : ObservableObject
{
    private static readonly Regex PhoneRegex = new(@"^\+?\d{8,15}$", RegexOptions.Compiled);
    private readonly IAuthService _auth;
    private string _savedPhone = "";

    public ProfileViewModel(IAuthService auth)
    {
        _auth = auth;
    }

    // ----- Données en lecture seule (venant des RH) -----
    [ObservableProperty] private string fullName = "";
    [ObservableProperty] private string subtitle = "";
    [ObservableProperty] private string hrLine = "";
    [ObservableProperty] private string arrivalLine = "";
    [ObservableProperty] private string managerLine = "";
    [ObservableProperty] private string email = "";
    [ObservableProperty] private bool isManager;

    // ----- Seule donnée modifiable : le téléphone -----
    [ObservableProperty,
     NotifyPropertyChangedFor(nameof(HasPhoneError)),
     NotifyCanExecuteChangedFor(nameof(SaveCommand))]
    private string phone = "";

    [ObservableProperty, NotifyPropertyChangedFor(nameof(HasPhoneError))]
    private string phoneError = "";

    [ObservableProperty, NotifyPropertyChangedFor(nameof(SaveText)), NotifyCanExecuteChangedFor(nameof(SaveCommand))]
    private bool isBusy;

    [ObservableProperty] private bool showSaved;
    [ObservableProperty] private bool hasSaveError;

    public bool HasPhoneError => !string.IsNullOrEmpty(PhoneError);
    public string SaveText => IsBusy ? "Chargement…" : "Enregistrer les modifications";

    // Appelée automatiquement à chaque frappe dans le champ téléphone.
    partial void OnPhoneChanged(string value)
    {
        PhoneError = Validate(value);
        ShowSaved = false;
        HasSaveError = false;
    }

    /// <summary>Recharge les données du profil à chaque affichage de l'onglet.</summary>
    public void Load()
    {
        var user = _auth.CurrentUser;
        if (user is null) return;

        FullName = user.FullName;
        Subtitle = $"{user.Role} · {user.Team}";
        HrLine = $"{user.EmployeeId} · {user.Contract} · {user.WeeklyHours} h / semaine";
        ArrivalLine = $"Arrivée : {FrFormat.LongDate(user.Arrival.ToDateTime(TimeOnly.MinValue))}";
        ManagerLine = $"Manager : {user.ManagerName}";
        Email = user.Email;
        IsManager = user.IsManager;

        _savedPhone = user.Phone;
        Phone = user.Phone;
        PhoneError = "";
        ShowSaved = false;
        HasSaveError = false;
    }

    // Le bouton n'est actif que si le numéro est valide ET différent de celui déjà enregistré.
    [RelayCommand(CanExecute = nameof(CanSave))]
    private async Task SaveAsync()
    {
        ShowSaved = false;
        HasSaveError = false;
        IsBusy = true;
        try
        {
            var value = Phone.Trim();
            await _auth.UpdatePhoneAsync(value);
            _savedPhone = value;
            ShowSaved = true;
        }
        catch (Exception)
        {
            HasSaveError = true;
        }
        finally
        {
            IsBusy = false;
            SaveCommand.NotifyCanExecuteChanged(); // le numéro enregistré a changé : réévaluer
        }
    }

    private bool CanSave() =>
        !IsBusy && string.IsNullOrEmpty(PhoneError) && Normalize(Phone) != Normalize(_savedPhone);

    [RelayCommand]
    private async Task OpenManagerAsync() => await Shell.Current.GoToAsync("manager");

    // Règle : déconnexion uniquement après confirmation de l'utilisateur.
    [RelayCommand]
    private async Task LogoutAsync()
    {
        var confirmed = await Shell.Current.DisplayAlertAsync(
            "Se déconnecter", "Voulez-vous vraiment vous déconnecter ?", "Se déconnecter", "Annuler");
        if (!confirmed) return;

        _auth.Logout();
        await Shell.Current.GoToAsync("//login");
    }

    private static string Normalize(string? value) =>
        new((value ?? "").Where(c => char.IsDigit(c) || c == '+').ToArray());

    private static string Validate(string? value)
    {
        var digits = Normalize(value);
        if (digits.Length == 0)
            return "Saisissez votre numéro de téléphone.";
        return PhoneRegex.IsMatch(digits) ? "" : "Saisissez un numéro de téléphone valide.";
    }
}

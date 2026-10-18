using System.Text.RegularExpressions;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using TaskifyHR.Models;
using TaskifyHR.Services;

namespace TaskifyHR.ViewModels;

public partial class LoginViewModel : ObservableObject, IRecipient<AccountCreatedMessage>
{
    private static readonly Regex EmailRegex = new(@"^[^@\s]+@[^@\s]+\.[^@\s]+$", RegexOptions.Compiled);
    private readonly IAuthService _auth;

    public LoginViewModel(IAuthService auth)
    {
        _auth = auth;
        WeakReferenceMessenger.Default.Register(this); // écoute "compte créé"
    }

    // Reçu depuis l'écran d'inscription : on préremplit l'e-mail du nouveau compte.
    public void Receive(AccountCreatedMessage message)
    {
        Email = message.Email;
        Password = "";
        EmailError = "";
        PasswordError = "";
        FormError = "";
    }

    // [ObservableProperty] génère automatiquement la propriété publique (Email, Password...)
    // et la notification de changement : plus besoin d'écrire INotifyPropertyChanged à la main.
    [ObservableProperty] private string email = "lea.martin@taskify.fr";
    [ObservableProperty] private string password = "";

    [ObservableProperty, NotifyPropertyChangedFor(nameof(HasEmailError))]
    private string emailError = "";

    [ObservableProperty, NotifyPropertyChangedFor(nameof(HasPasswordError))]
    private string passwordError = "";

    [ObservableProperty, NotifyPropertyChangedFor(nameof(HasFormError)), NotifyPropertyChangedFor(nameof(ButtonText))]
    private string formError = "";

    [ObservableProperty, NotifyPropertyChangedFor(nameof(ButtonText)), NotifyCanExecuteChangedFor(nameof(LoginCommand))]
    private bool isBusy;

    [ObservableProperty, NotifyPropertyChangedFor(nameof(PasswordToggleText))]
    private bool isPasswordHidden = true;

    public bool HasEmailError => !string.IsNullOrEmpty(EmailError);
    public bool HasPasswordError => !string.IsNullOrEmpty(PasswordError);
    public bool HasFormError => !string.IsNullOrEmpty(FormError);
    public string PasswordToggleText => IsPasswordHidden ? "Afficher" : "Masquer";

    // Le texte du bouton suit l'état : normal / chargement / après une erreur
    public string ButtonText => IsBusy ? "Chargement…" : HasFormError ? "Réessayer" : "Se connecter";

    [RelayCommand]
    private void TogglePassword() => IsPasswordHidden = !IsPasswordHidden;

    [RelayCommand(CanExecute = nameof(CanLogin))]
    private async Task LoginAsync()
    {
        EmailError = "";
        PasswordError = "";
        FormError = "";

        // 1) Validation des champs (avant tout appel réseau)
        if (string.IsNullOrWhiteSpace(Email))
            EmailError = "Saisissez votre adresse e-mail.";
        else if (!EmailRegex.IsMatch(Email.Trim()))
            EmailError = "Saisissez une adresse e-mail valide.";

        if (string.IsNullOrEmpty(Password))
            PasswordError = "Saisissez votre mot de passe.";

        if (HasEmailError || HasPasswordError)
            return;

        // 2) Connexion
        IsBusy = true;
        try
        {
            var ok = await _auth.LoginAsync(Email, Password);
            if (ok)
            {
                Password = "";
                await Shell.Current.GoToAsync("//main/home"); // route absolue : onglet Accueil
            }
            else
            {
                FormError = "Adresse e-mail ou mot de passe incorrect.";
                PasswordError = "Vérifiez vos identifiants.";
            }
        }
        catch (Exception)
        {
            FormError = "Connexion impossible. Vérifiez votre réseau et réessayez.";
        }
        finally
        {
            IsBusy = false;
        }
    }

    private bool CanLogin() => !IsBusy;

    [RelayCommand]
    private async Task ForgotPasswordAsync() =>
        await Shell.Current.DisplayAlert("Mot de passe oublié", "Cet écran sera ajouté prochainement.", "OK");

    [RelayCommand]
    private async Task GoToRegisterAsync() =>
        await Shell.Current.GoToAsync("register");
}

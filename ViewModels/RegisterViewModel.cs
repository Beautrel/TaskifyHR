using System.Text.RegularExpressions;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using TaskifyHR.Models;
using TaskifyHR.Services;

namespace TaskifyHR.ViewModels;

public partial class RegisterViewModel : ObservableObject
{
    private static readonly Regex EmailRegex = new(@"^[^@\s]+@[^@\s]+\.[^@\s]+$", RegexOptions.Compiled);
    private static readonly Regex EmployeeIdRegex = new(@"^[A-Za-z]{1,4}-\d{2,6}$", RegexOptions.Compiled);

    private readonly IAuthService _auth;
    private bool _valid;

    public RegisterViewModel(IAuthService auth)
    {
        _auth = auth;
    }

    // ----- Champs du formulaire -----
    [ObservableProperty] private string fullName = "";
    [ObservableProperty] private string email = "";
    [ObservableProperty] private string employeeId = "";
    [ObservableProperty] private string password = "";
    [ObservableProperty] private string confirmPassword = "";

    // ----- Messages d'erreur par champ (vides = champ correct ou pas encore rempli) -----
    [ObservableProperty, NotifyPropertyChangedFor(nameof(HasFullNameError))] private string fullNameError = "";
    [ObservableProperty, NotifyPropertyChangedFor(nameof(HasEmailError))] private string emailError = "";
    [ObservableProperty, NotifyPropertyChangedFor(nameof(HasEmployeeIdError))] private string employeeIdError = "";
    [ObservableProperty, NotifyPropertyChangedFor(nameof(HasPasswordError))] private string passwordError = "";
    [ObservableProperty, NotifyPropertyChangedFor(nameof(HasConfirmError))] private string confirmError = "";
    [ObservableProperty, NotifyPropertyChangedFor(nameof(HasServerError))] private string serverError = "";

    [ObservableProperty] private bool hasAnyError;

    [ObservableProperty, NotifyCanExecuteChangedFor(nameof(SubmitCommand))]
    private bool canSubmit;

    [ObservableProperty, NotifyPropertyChangedFor(nameof(SubmitText))]
    private bool isBusy;

    [ObservableProperty, NotifyPropertyChangedFor(nameof(IsFormVisible))]
    private bool isCreated;

    public bool HasFullNameError => !string.IsNullOrEmpty(FullNameError);
    public bool HasEmailError => !string.IsNullOrEmpty(EmailError);
    public bool HasEmployeeIdError => !string.IsNullOrEmpty(EmployeeIdError);
    public bool HasPasswordError => !string.IsNullOrEmpty(PasswordError);
    public bool HasConfirmError => !string.IsNullOrEmpty(ConfirmError);
    public bool HasServerError => !string.IsNullOrEmpty(ServerError);
    public bool IsFormVisible => !IsCreated;
    public string SubmitText => IsBusy ? "Chargement…" : "Créer mon compte";

    // Validation "en direct" : chaque frappe relance Recompute() (appelé automatiquement).
    partial void OnFullNameChanged(string value) => Recompute();
    partial void OnEmailChanged(string value) => Recompute();
    partial void OnEmployeeIdChanged(string value) => Recompute();
    partial void OnPasswordChanged(string value) => Recompute();
    partial void OnConfirmPasswordChanged(string value) => Recompute();
    partial void OnIsBusyChanged(bool value) => CanSubmit = _valid && !value;

    private void Recompute()
    {
        ServerError = "";

        // Une erreur n'apparaît que sur un champ déjà rempli (pas de rouge sur un formulaire vierge).
        FullNameError = Check(FullName, v => v.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries).Length >= 2,
                              "Saisissez votre prénom et votre nom.");
        EmailError = Check(Email, v => EmailRegex.IsMatch(v.Trim()), "Saisissez une adresse e-mail valide.");
        EmployeeIdError = Check(EmployeeId, v => EmployeeIdRegex.IsMatch(v.Trim()), "Format attendu : TF-042.");
        PasswordError = PasswordRule(Password);
        ConfirmError = string.IsNullOrEmpty(ConfirmPassword) || ConfirmPassword == Password
            ? ""
            : "Les mots de passe ne correspondent pas.";

        HasAnyError = HasFullNameError || HasEmailError || HasEmployeeIdError || HasPasswordError || HasConfirmError;

        // Formulaire valide = tous les champs remplis ET aucune erreur
        var allFilled = !string.IsNullOrWhiteSpace(FullName) && !string.IsNullOrWhiteSpace(Email)
                        && !string.IsNullOrWhiteSpace(EmployeeId) && !string.IsNullOrEmpty(Password)
                        && !string.IsNullOrEmpty(ConfirmPassword);
        _valid = allFilled && !HasAnyError;
        CanSubmit = _valid && !IsBusy;
    }

    [RelayCommand(CanExecute = nameof(CanSubmit))]
    private async Task SubmitAsync()
    {
        ServerError = "";
        IsBusy = true; // désactive le bouton : pas de double envoi
        try
        {
            await _auth.RegisterAsync(new RegistrationRequest(
                FullName.Trim(), Email.Trim(), EmployeeId.Trim(), Password));
            IsCreated = true;
        }
        catch (InvalidOperationException ex) // e-mail ou matricule déjà utilisé
        {
            ServerError = ex.Message;
        }
        catch (Exception)
        {
            ServerError = "Inscription impossible. Vérifiez votre connexion et réessayez.";
        }
        finally
        {
            IsBusy = false;
        }
    }

    // Retour à la connexion : l'e-mail du nouveau compte est envoyé à l'écran de connexion.
    [RelayCommand]
    private async Task GoToLoginAsync()
    {
        if (IsCreated)
            WeakReferenceMessenger.Default.Send(new AccountCreatedMessage(Email.Trim()));
        await Shell.Current.GoToAsync("..");
    }

    private static string Check(string? value, Func<string, bool> isValid, string message) =>
        string.IsNullOrWhiteSpace(value) || isValid(value) ? "" : message;

    // Règle : 12 caractères, une majuscule et un chiffre
    private static string PasswordRule(string? value)
    {
        if (string.IsNullOrEmpty(value)) return "";
        if (value.Length < 12) return "12 caractères minimum.";
        if (!value.Any(char.IsUpper)) return "Ajoutez une majuscule.";
        if (!value.Any(char.IsDigit)) return "Ajoutez un chiffre.";
        return "";
    }
}

using TaskifyHR.Services;

namespace TaskifyHR.Views;

// Pages provisoires pour que les 4 onglets existent déjà.
// On les remplace une par une par de vrais écrans (XAML + ViewModel).

public abstract class PlaceholderPage : ContentPage
{
    protected PlaceholderPage(string title)
    {
        Title = title;
        Content = new VerticalStackLayout
        {
            Padding = 16,
            Spacing = 8,
            Children =
            {
                new Label { Text = title, FontSize = 24, FontAttributes = FontAttributes.Bold },
                new Label { Text = "Écran à venir…" }
            }
        };
    }
}

public class PlanningPage : PlaceholderPage
{
    public PlanningPage() : base("Planning") { }
}

// Profil provisoire : contient déjà "Espace manager ›" (visible uniquement pour un manager)
// et "Se déconnecter", pour pouvoir tester tous les parcours.
public class ProfilePage : ContentPage
{
    private readonly IAuthService _auth;
    private readonly Button _managerButton;

    public ProfilePage(IAuthService auth)
    {
        _auth = auth;
        Title = "Profil";

        _managerButton = new Button { Text = "Espace manager ›", IsVisible = false };
        _managerButton.Clicked += async (_, _) => await Shell.Current.GoToAsync("manager");
        UseStyle(_managerButton, "SecondaryButton");

        var logout = new Button { Text = "Se déconnecter" };
        logout.Clicked += async (_, _) =>
        {
            auth.Logout();
            await Shell.Current.GoToAsync("//login");
        };
        UseStyle(logout, "SecondaryButton");

        Content = new VerticalStackLayout
        {
            Padding = 16,
            Spacing = 16,
            Children =
            {
                new Label { Text = "Profil", FontSize = 24, FontAttributes = FontAttributes.Bold },
                new Label { Text = "Écran à venir…" },
                _managerButton,
                logout
            }
        };
    }

    // Règle : l'espace manager n'est proposé qu'au manager.
    protected override void OnAppearing()
    {
        base.OnAppearing();
        _managerButton.IsVisible = _auth.CurrentUser?.IsManager == true;
    }

    private static void UseStyle(Button button, string key)
    {
        if (Application.Current is { } app && app.Resources.TryGetValue(key, out var value) && value is Style style)
            button.Style = style;
    }
}

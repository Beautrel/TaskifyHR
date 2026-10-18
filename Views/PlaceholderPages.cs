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

// Le profil contient déjà "Se déconnecter" pour pouvoir tester la boucle connexion → accueil → connexion.
public class ProfilePage : ContentPage
{
    public ProfilePage(IAuthService auth)
    {
        Title = "Profil";

        var logout = new Button { Text = "Se déconnecter" };
        logout.Clicked += async (_, _) =>
        {
            auth.Logout();
            await Shell.Current.GoToAsync("//login");
        };

        Content = new VerticalStackLayout
        {
            Padding = 16,
            Spacing = 16,
            Children =
            {
                new Label { Text = "Profil", FontSize = 24, FontAttributes = FontAttributes.Bold },
                new Label { Text = "Écran à venir…" },
                logout
            }
        };
    }
}

namespace TaskifyHR.Views;

// Page provisoire pour que l'onglet Planning existe déjà.
// On la remplacera par un vrai écran (XAML + ViewModel).
public class PlanningPage : ContentPage
{
    public PlanningPage()
    {
        Title = "Planning";
        Content = new VerticalStackLayout
        {
            Padding = 16,
            Spacing = 8,
            Children =
            {
                new Label { Text = "Planning", FontSize = 24, FontAttributes = FontAttributes.Bold },
                new Label { Text = "Écran à venir…" }
            }
        };
    }
}

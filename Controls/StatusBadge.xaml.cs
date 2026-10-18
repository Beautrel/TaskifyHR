using TaskifyHR.Models;

namespace TaskifyHR.Controls;

public partial class StatusBadge : ContentView
{
    public static readonly BindableProperty StatusProperty =
        BindableProperty.Create(nameof(Status), typeof(LeaveStatus), typeof(StatusBadge), LeaveStatus.Pending,
            propertyChanged: (bindable, _, _) => ((StatusBadge)bindable).Refresh());

    public StatusBadge()
    {
        InitializeComponent();
        Refresh();
    }

    public LeaveStatus Status
    {
        get => (LeaveStatus)GetValue(StatusProperty);
        set => SetValue(StatusProperty, value);
    }

    // Texte et couleurs selon le statut (les couleurs viennent de Colors.xaml).
    private void Refresh()
    {
        var (text, background, foreground) = Status switch
        {
            LeaveStatus.Pending => ("En attente", "Warning", "OnWarning"),
            LeaveStatus.Approved => ("Approuvé", "Success", "White"),
            _ => ("Refusé", "Error", "White")
        };

        TextLabel.Text = text;
        TextLabel.TextColor = GetColor(foreground);
        Badge.BackgroundColor = GetColor(background);
    }

    private static Color GetColor(string key) =>
        Application.Current is { } app && app.Resources.TryGetValue(key, out var value) && value is Color color
            ? color
            : Colors.Gray;
}

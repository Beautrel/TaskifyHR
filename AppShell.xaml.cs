using TaskifyHR.Views;

namespace TaskifyHR;

public partial class AppShell : Shell
{
    public AppShell()
    {
        InitializeComponent();

        // Pages ouvertes "par-dessus" les onglets : on les déclare ici,
        // puis on y navigue avec Shell.Current.GoToAsync("leave-request").
        Routing.RegisterRoute("leave-request", typeof(LeaveRequestPage));
    }
}

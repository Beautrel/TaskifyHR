using TaskifyHR.Views;

namespace TaskifyHR;

public partial class AppShell : Shell
{
    public AppShell()
    {
        InitializeComponent();

        // Pages ouvertes "par-dessus" les onglets : on les déclare ici,
        // puis on y navigue avec Shell.Current.GoToAsync("<route>").
        Routing.RegisterRoute("leave-request", typeof(LeaveRequestPage));
        Routing.RegisterRoute("manager", typeof(ManagerPage));
        Routing.RegisterRoute("register", typeof(RegisterPage));
        Routing.RegisterRoute("export", typeof(ExportPage));
    }
}

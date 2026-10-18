namespace TaskifyHR;

public partial class AppShell : Shell
{
    public AppShell()
    {
        InitializeComponent();

        // Pages ouvertes "par-dessus" les onglets (Demande de congé, etc.) :
        // on les déclarera ici avec Routing.RegisterRoute(...) quand elles existeront.
    }
}

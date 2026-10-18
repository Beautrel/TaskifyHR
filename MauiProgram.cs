using Microsoft.Extensions.Logging;
using TaskifyHR.Services;
using TaskifyHR.ViewModels;
using TaskifyHR.Views;

namespace TaskifyHR;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder.UseMauiApp<App>();

        // Android : retire le soulignement par défaut des champs de saisie
        // (le champ est dessiné par la bordure du style "Field").
        Microsoft.Maui.Handlers.EntryHandler.Mapper.AppendToMapping("NoUnderline", (handler, view) =>
        {
#if ANDROID
            handler.PlatformView.BackgroundTintList =
                Android.Content.Res.ColorStateList.ValueOf(Android.Graphics.Color.Transparent);
#endif
        });

        // Services (données fictives pour l'instant : on branchera une vraie API plus tard)
        builder.Services.AddSingleton<IAuthService, MockAuthService>();
        builder.Services.AddSingleton<ILeaveService, MockLeaveService>();
        builder.Services.AddSingleton<IAttendanceService, MockAttendanceService>();
        builder.Services.AddSingleton<ILocationService, DeviceLocationService>();

        // ViewModels
        builder.Services.AddTransient<LoginViewModel>();
        builder.Services.AddTransient<HomeViewModel>();
        builder.Services.AddTransient<BalanceViewModel>();

        // Pages
        builder.Services.AddTransient<LoginPage>();
        builder.Services.AddTransient<HomePage>();
        builder.Services.AddTransient<BalancePage>();
        builder.Services.AddTransient<PlanningPage>();
        builder.Services.AddTransient<ProfilePage>();

#if DEBUG
        builder.Logging.AddDebug();
#endif

        return builder.Build();
    }
}

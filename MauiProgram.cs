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

        // Android : retire le soulignement par défaut des champs de saisie, listes et sélecteurs de date
        // (le champ est dessiné par la bordure du style "Field").
        Microsoft.Maui.Handlers.EntryHandler.Mapper.AppendToMapping("NoUnderline", (handler, view) =>
        {
#if ANDROID
            handler.PlatformView.BackgroundTintList =
                Android.Content.Res.ColorStateList.ValueOf(Android.Graphics.Color.Transparent);
#endif
        });
        Microsoft.Maui.Handlers.DatePickerHandler.Mapper.AppendToMapping("NoUnderline", (handler, view) =>
        {
#if ANDROID
            handler.PlatformView.BackgroundTintList =
                Android.Content.Res.ColorStateList.ValueOf(Android.Graphics.Color.Transparent);
#endif
        });
        Microsoft.Maui.Handlers.PickerHandler.Mapper.AppendToMapping("NoUnderline", (handler, view) =>
        {
#if ANDROID
            handler.PlatformView.BackgroundTintList =
                Android.Content.Res.ColorStateList.ValueOf(Android.Graphics.Color.Transparent);
#endif
        });

        // Services : vraie API Django (ApiConfig.UseApi = true) ou données fictives (secours)
        if (ApiConfig.UseApi)
        {
            builder.Services.AddSingleton<ApiClient>();
            builder.Services.AddSingleton<IAuthService, ApiAuthService>();
            builder.Services.AddSingleton<ILeaveService, ApiLeaveService>();
            builder.Services.AddSingleton<IAttendanceService, ApiAttendanceService>();
        }
        else
        {
            builder.Services.AddSingleton<IAuthService, MockAuthService>();
            builder.Services.AddSingleton<ILeaveService, MockLeaveService>();
            builder.Services.AddSingleton<IAttendanceService, MockAttendanceService>();
        }
        builder.Services.AddSingleton<ILocationService, DeviceLocationService>();
        builder.Services.AddSingleton<IExportService, FileExportService>();

        // ViewModels
        builder.Services.AddTransient<LoginViewModel>();
        builder.Services.AddTransient<HomeViewModel>();
        builder.Services.AddTransient<BalanceViewModel>();
        builder.Services.AddTransient<LeaveRequestViewModel>();
        builder.Services.AddTransient<ManagerViewModel>();
        builder.Services.AddTransient<ProfileViewModel>();
        builder.Services.AddTransient<PlanningViewModel>();
        builder.Services.AddTransient<RegisterViewModel>();
        builder.Services.AddTransient<ExportViewModel>();

        // Pages
        builder.Services.AddTransient<LoginPage>();
        builder.Services.AddTransient<HomePage>();
        builder.Services.AddTransient<BalancePage>();
        builder.Services.AddTransient<PlanningPage>();
        builder.Services.AddTransient<ProfilePage>();
        builder.Services.AddTransient<LeaveRequestPage>();
        builder.Services.AddTransient<ManagerPage>();
        builder.Services.AddTransient<RegisterPage>();
        builder.Services.AddTransient<ExportPage>();

#if DEBUG
        builder.Logging.AddDebug();
#endif

        return builder.Build();
    }
}

using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Devices.Sensors;

namespace TaskifyHR.Services;

/// <summary>
/// Géolocalisation ponctuelle : la position n'est demandée QU'AU CLIC de pointage
/// (jamais de suivi continu), ce qui économise la batterie et protège la vie privée.
/// </summary>
public class DeviceLocationService : ILocationService
{
    // true = on vérifie les permissions et le GPS, mais on ne bloque pas selon la distance
    // (pratique pour la démo hors du lieu de travail). Mettre false pour activer la vérification de zone.
    private static readonly bool DemoMode = true;

    private static readonly Location Site = new(4.0511, 9.7679); // lieu de travail fictif
    private const double RadiusKm = 0.2;

    public async Task<LocationCheck> CheckAsync()
    {
        var status = await Permissions.CheckStatusAsync<Permissions.LocationWhenInUse>();
        if (status != PermissionStatus.Granted)
            status = await Permissions.RequestAsync<Permissions.LocationWhenInUse>();
        if (status != PermissionStatus.Granted)
            return LocationCheck.PermissionDenied;

        try
        {
            var request = new GeolocationRequest(GeolocationAccuracy.Medium, TimeSpan.FromSeconds(10));
            var position = await Geolocation.Default.GetLocationAsync(request);
            if (position is null)
                return LocationCheck.Unavailable;

            if (DemoMode)
                return LocationCheck.InZone;

            var km = Location.CalculateDistance(position, Site, DistanceUnits.Kilometers);
            return km <= RadiusKm ? LocationCheck.InZone : LocationCheck.OutOfZone;
        }
        catch (PermissionException)
        {
            return LocationCheck.PermissionDenied;
        }
        catch (Exception) // GPS désactivé, délai dépassé, appareil sans GPS...
        {
            return LocationCheck.Unavailable;
        }
    }
}

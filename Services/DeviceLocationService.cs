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

    /// <summary>Dernière position lue (envoyée à l'API au pointage).</summary>
    public static Location? LastPosition { get; private set; }

    /// <summary>
    /// Position envoyée au serveur. En mode démo, on envoie celle du lieu de travail
    /// (le serveur refuse sinon tout pointage fait hors de la zone de 200 m).
    /// </summary>
    public static (double Latitude, double Longitude) ServerPosition =>
        DemoMode || LastPosition is null
            ? (Site.Latitude, Site.Longitude)
            : (LastPosition.Latitude, LastPosition.Longitude);

    public async Task<LocationCheck> CheckAsync()
    {
#if WINDOWS
        return LocationCheck.InZone;
#endif
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

            LastPosition = position;
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

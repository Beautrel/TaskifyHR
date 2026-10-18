using System.Globalization;

namespace TaskifyHR.Helpers;

/// <summary>Formats de dates en français, réutilisés par plusieurs écrans.</summary>
public static class FrFormat
{
    private static readonly CultureInfo Fr = new("fr-FR");

    public static string LongDate(DateTime date) => date.ToString("d MMMM yyyy", Fr);

    public static string ShortDate(DateTime date) => date.ToString("d MMMM", Fr);

    public static string DateRange(DateOnly start, DateOnly end)
    {
        if (start == end)
            return start.ToString("d MMMM yyyy", Fr);
        if (start.Year == end.Year && start.Month == end.Month)
            return $"{start.Day} – {end.ToString("d MMMM yyyy", Fr)}";
        if (start.Year == end.Year)
            return $"{start.ToString("d MMMM", Fr)} – {end.ToString("d MMMM yyyy", Fr)}";
        return $"{start.ToString("d MMMM yyyy", Fr)} – {end.ToString("d MMMM yyyy", Fr)}";
    }
}

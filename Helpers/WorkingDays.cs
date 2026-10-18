namespace TaskifyHR.Helpers;

public static class WorkingDays
{
    /// <summary>Nombre de jours ouvrés (lundi à vendredi) entre deux dates, bornes incluses.</summary>
    public static int Count(DateOnly start, DateOnly end)
    {
        var count = 0;
        for (var day = start; day <= end; day = day.AddDays(1))
        {
            if (day.DayOfWeek is not DayOfWeek.Saturday and not DayOfWeek.Sunday)
                count++;
        }
        return count;
    }
}

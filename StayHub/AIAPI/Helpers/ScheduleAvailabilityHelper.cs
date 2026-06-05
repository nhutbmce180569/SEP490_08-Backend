namespace AIAPI.Helpers;

public static class ScheduleAvailabilityHelper
{
    public const int NearbyWindowDays = 30;
    public const int ExtendedWindowDays = 90;

    public static DateTime ResolveWindowEnd(DateTime start, DateTime? end) =>
        (end ?? start.AddDays(30)).Date;

    public static bool IsInPreferredWindow(DateTime? departure, DateTime start, DateTime end)
    {
        if (!departure.HasValue)
        {
            return true;
        }

        var d = departure.Value.Date;
        return d >= start.Date && d <= end.Date;
    }

    public static int DaysOutsideWindow(DateTime? departure, DateTime start, DateTime end)
    {
        if (!departure.HasValue)
        {
            return 0;
        }

        var d = departure.Value.Date;
        if (d >= start.Date && d <= end.Date)
        {
            return 0;
        }

        return d < start.Date
            ? (start.Date - d).Days
            : (d - end.Date).Days;
    }

    public static bool IsNearby(DateTime? departure, DateTime start, DateTime end) =>
        IsWithinDaysOutside(departure, start, end, NearbyWindowDays);

    public static bool IsExtendedNearby(DateTime? departure, DateTime start, DateTime end) =>
        IsWithinDaysOutside(departure, start, end, ExtendedWindowDays);

    public static bool IsWithinDaysOutside(
        DateTime? departure,
        DateTime start,
        DateTime end,
        int maxDaysOutside)
    {
        if (!departure.HasValue)
        {
            return false;
        }

        var days = DaysOutsideWindow(departure, start, end);
        return days > 0 && days <= maxDaysOutside;
    }
}

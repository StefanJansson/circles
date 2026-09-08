using System.Globalization;

namespace Circles.Web.Shared;

/// <summary>
/// Swedish date/time formatting helpers so every screen shows dates the same way.
/// All methods use the sv-SE culture and render local time.
/// </summary>
public static class DateHelper
{
    private static readonly CultureInfo Sv = CultureInfo.GetCultureInfo("sv-SE");

    /// <summary>Long date, e.g. "8 september 2026".</summary>
    public static string LongDate(DateTime value) =>
        value.ToLocalTime().ToString("d MMMM yyyy", Sv);

    /// <summary>Short date, e.g. "08 sep".</summary>
    public static string ShortDate(DateTime value) =>
        value.ToLocalTime().ToString("dd MMM", Sv);

    /// <summary>Date + time, e.g. "8 sep 2026, 14:30".</summary>
    public static string DateTime(DateTime value) =>
        value.ToLocalTime().ToString("d MMM yyyy, HH:mm", Sv);

    /// <summary>Weekday + date, e.g. "lör 18 jan".</summary>
    public static string WeekdayDate(DateTime value) =>
        value.ToLocalTime().ToString("ddd d MMM", Sv);

    /// <summary>Time only, e.g. "14:30".</summary>
    public static string Time(DateTime value) =>
        value.ToLocalTime().ToString("HH:mm", Sv);
}

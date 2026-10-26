namespace JobForge.Ui.Services;

public static class RelativeTime
{
    public static (string Key, int? Value) Format(DateTime timestampUtc, DateTime nowUtc)
    {
        var value = nowUtc - (timestampUtc.Kind == DateTimeKind.Utc ? timestampUtc : timestampUtc.ToUniversalTime());
        if (value.TotalSeconds < 5) return ("time_now", null);
        if (value.TotalMinutes < 1) return ("time_seconds_ago", (int)value.TotalSeconds);
        if (value.TotalHours < 1) return ("time_minutes_ago", (int)value.TotalMinutes);
        if (value.TotalDays < 1) return ("time_hours_ago", (int)value.TotalHours);
        return ("time_days_ago", (int)value.TotalDays);
    }
}

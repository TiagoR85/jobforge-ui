using Cronos;

namespace JobForge.Ui.Services;

public static class CronPreview
{
    public static IReadOnlyList<DateTime> Next(string cron, DateTime fromUtc, int count)
    {
        if (string.IsNullOrWhiteSpace(cron) || count <= 0)
        {
            return [];
        }

        try
        {
            var expression = CronExpression.Parse(cron.Trim());
            var result = new List<DateTime>(count);
            var cursor = DateTime.SpecifyKind(fromUtc, DateTimeKind.Utc);
            for (var i = 0; i < count; i++)
            {
                var occurrence = expression.GetNextOccurrence(cursor, TimeZoneInfo.Utc);
                if (occurrence is null) break;
                var value = DateTime.SpecifyKind(occurrence.Value, DateTimeKind.Utc);
                result.Add(value);
                cursor = value;
            }
            return result;
        }
        catch (CronFormatException)
        {
            return [];
        }
    }
}

using System;

namespace PRCat.Core.Helpers
{
    public static class TimeExtensions
    {
        public static int GetEpochSeconds(this DateTime date)
        {
            TimeSpan t = date - new DateTime(1970, 1, 1);
            return (int)t.TotalSeconds;
        }

        public static DateTime FromEpochSeconds(this DateTime date, long EpochSeconds)
        {
            var epoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            return epoch.AddSeconds(EpochSeconds);

        }

        public static DateTimeOffset ToEasternTime(this DateTimeOffset datetime)
        {
            return TimeZoneInfo.ConvertTime(datetime, TimeZoneInfo.FindSystemTimeZoneById("Eastern Standard Time"));
        }

        
    }
}
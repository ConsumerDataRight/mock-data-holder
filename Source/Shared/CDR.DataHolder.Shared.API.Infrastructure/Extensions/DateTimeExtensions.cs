using System;

namespace CDR.DataHolder.Shared.API.Infrastructure.Extensions
{
    public static class DateTimeExtensions
    {
        private static readonly DateTime _epochTime = new DateTime(1970, 1, 1);

        public static bool HasExpired(this DateTime creationTime, int seconds, DateTime now)
            => creationTime.AddSeconds(seconds) < now;

        public static int ToEpoch(this DateTime time) => (int)(time - _epochTime).TotalSeconds;

        public static DateTime FromEpoch(this int time) => DateTimeOffset.FromUnixTimeSeconds(time).UtcDateTime;
    }
}
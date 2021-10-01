using System;

namespace CDR.DataHolder.IntegrationTests.Extensions
{
    public static class DateTimeExtensions
    {
        /// <summary>
        /// Return datetime converted to Unix Epoch (number of seconds since 00:00:00 UTC on 1 Jan 1970)
        /// </summary>
        public static int UnixEpoch(this DateTime datetime)
        {
            return Convert.ToInt32(datetime.Subtract(DateTime.UnixEpoch).TotalSeconds);
        }
    }

}
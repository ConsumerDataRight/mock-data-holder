using System;

namespace CDR.DataHolder.IntegrationTests.Extensions
{
    public static class StringExtensions
    {
        /// <summary>
        /// Convert string to int
        /// </summary>
        public static int ToInt(this string str)
        {
            return Convert.ToInt32(str);
        }
    }
}
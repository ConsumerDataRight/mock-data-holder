using System.Globalization;

namespace CDR.DataHolder.Shared.Domain.Extensions
{
    public static class StringExtensions
    {
        public static bool IsBanking(this string industry)
        {
            return industry.CompareTo(Constants.Industry.Banking) == 0;
        }

        public static bool IsEnergy(this string industry)
        {
            return industry.CompareTo(Constants.Industry.Energy) == 0;
        }
        public static bool IsNullOrWhiteSpace(this string str)
        {
            return string.IsNullOrWhiteSpace(str);
        }

        public static bool TryParseToDateTime(this string stringDateTime, out DateTime? dateTime)
        {
            dateTime = default;

            if (stringDateTime.IsNullOrWhiteSpace())
            {
                return false;
            }

            var provider = new CultureInfo("en-US");
            if (DateTime.TryParse(stringDateTime, provider, DateTimeStyles.None, out var parsedDate))
            {
                dateTime = parsedDate;
                return true;
            }

            return false;
        }
    }
}

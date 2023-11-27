namespace CDR.DataHolder.Shared.Domain
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
    }
}

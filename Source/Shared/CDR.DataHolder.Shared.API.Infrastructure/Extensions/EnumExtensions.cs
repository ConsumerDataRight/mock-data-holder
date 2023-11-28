using System;
using System.ComponentModel;
using System.Globalization;
using System.Linq;

namespace CDR.DataHolder.Shared.API.Infrastructure.Extensions
{
    public static class EnumExtensions
    {
        public static string? GetDescription<T>(this T e)
            where T : IConvertible
        {
            if (e is Enum)
            {
                Type type = e.GetType();
                Array values = System.Enum.GetValues(type);

                foreach (int val in values)
                {
                    if (val == e.ToInt32(CultureInfo.InvariantCulture))
                    {
                        var memInfo = type.GetMember(type.GetEnumName(val)!);
                        var descriptionAttribute = memInfo[0]
                            .GetCustomAttributes(typeof(DescriptionAttribute), false)
                            .FirstOrDefault() as DescriptionAttribute;

                        if (descriptionAttribute != null)
                        {
                            return descriptionAttribute.Description;
                        }
                    }
                }
            }

            return null; // could also return string.Empty
        }

        public static string? GetName<T>(this T @enum)
            where T : Enum
            => Enum.GetName(typeof(T), @enum);

        public static string ToInt32String(this Enum @enum)
            => Convert.ToInt32(@enum, CultureInfo.CurrentCulture).ToString(CultureInfo.CurrentCulture);
    }
}

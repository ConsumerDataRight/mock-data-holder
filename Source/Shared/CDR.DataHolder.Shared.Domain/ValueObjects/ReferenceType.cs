namespace CDR.DataHolder.Shared.Domain.ValueObjects
{
    public abstract class ReferenceType<T_ID, T_CODE>
        where T_ID : Enum
    {
        public static bool IsValid(IDictionary<T_ID, T_CODE> values, string code)
        {
            var reference = GetValueByCode(values, code);
            return !Enum.Equals(reference.Key, default(T_ID));
        }

        private static KeyValuePair<T_ID, T_CODE> GetValueByCode(IDictionary<T_ID, T_CODE> values, string code)
        {
            if (string.IsNullOrEmpty(code))
            {
                return default(KeyValuePair<T_ID, T_CODE>);
            }

            return values.FirstOrDefault(category => category.Value?.ToString() == code.ToUpperInvariant());
        }

        public static T_ID GetIdByCode(IDictionary<T_ID, T_CODE> values, string code)
        {
            var reference = GetValueByCode(values, code);
            return reference.Key;
        }
    }
}

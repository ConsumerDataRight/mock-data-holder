using System;
using System.Collections.Generic;
using System.Linq;

namespace CDR.DataHolder.Shared.Domain.ValueObjects
{
	public abstract class ReferenceType<ID, CODE> where ID : Enum
	{
		public static bool IsValid(IDictionary<ID, CODE> values, string code)
		{
			var reference = GetValueByCode(values, code);
			return !Enum.Equals(reference.Key, default(ID));
		}

		private static KeyValuePair<ID, CODE> GetValueByCode(IDictionary<ID, CODE> values, string code)
		{
			if (string.IsNullOrEmpty(code))
			{
				return default(KeyValuePair<ID, CODE>);
			}

			return values.FirstOrDefault(category => category.Value?.ToString() == code.ToUpperInvariant());
		}

		public static ID GetIdByCode(IDictionary<ID, CODE> values, string code)
		{
			var reference = GetValueByCode(values, code);
			return reference.Key;
		}
	}
}
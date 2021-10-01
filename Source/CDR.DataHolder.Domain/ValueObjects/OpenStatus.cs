using System.Collections.Generic;

namespace CDR.DataHolder.Domain.ValueObjects
{
	public class AccountOpenStatus : ReferenceType<OpenStatusEnum, string>
	{
		public OpenStatusEnum Id { get; set; }
		public string Code { get; set; }

		public static IDictionary<OpenStatusEnum, string> Values
		{
			get
			{
				return new Dictionary<OpenStatusEnum, string>
				{
					{OpenStatusEnum.All, "ALL"  },
					{OpenStatusEnum.Open, "OPEN"  },
					{OpenStatusEnum.Closed, "CLOSED"  }
				};
			}
		}
	}

	public enum OpenStatusEnum
	{
		Unknown = 0,
		Open = 1,
		Closed = 2,
		All = 3,
	}
}

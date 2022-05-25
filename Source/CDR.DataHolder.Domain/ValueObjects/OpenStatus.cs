﻿using System.Collections.Generic;

namespace CDR.DataHolder.Domain.ValueObjects
{
	public class AccountOpenStatus : ReferenceType<OpenStatus, string>
	{
		public OpenStatus Id { get; set; }
		public string Code { get; set; }

		public static IDictionary<OpenStatus, string> Values
		{
			get
			{
				return new Dictionary<OpenStatus, string>
				{
					{OpenStatus.All, "ALL"  },
					{OpenStatus.Open, "OPEN"  },
					{OpenStatus.Closed, "CLOSED"  }
				};
			}
		}
	}

	public enum OpenStatus
	{
		Unknown = 0,
		Open = 1,
		Closed = 2,
		All = 3,
	}
}

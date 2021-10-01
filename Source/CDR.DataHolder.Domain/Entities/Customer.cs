using System;

namespace CDR.DataHolder.Domain.Entities
{
	public class Customer
	{
		public Guid CustomerId { get; set; }
		public string LoginId { get; set; }

		public string CustomerUType { get; set; }

		public Account[] Accounts { get; set; }
	}
}

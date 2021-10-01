using System;
using CDR.DataHolder.Domain.ValueObjects;

namespace CDR.DataHolder.Domain.Entities
{
	public class Account
	{
		public string AccountId { get; set; }
		public string CustomerId { get; set; }
		public DateTime? CreationDate { get; set; }
		public string DisplayName { get; set; }
		public string NickName { get; set; }
		public string OpenStatus { get; set; }
		public string MaskedName { get; set; }
		public string ProductCategory { get; set; }
		public string ProductName { get; set; }
		public AccountTransaction[] Transactions { get; set; }
	}
}
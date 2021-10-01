using System;

namespace CDR.DataHolder.Domain.Entities
{
	public class AccountTransaction
	{
		public string TransactionId { get; set; }

		public string TransactionType { get; set; }

		public string Status { get; set; }

		public string Description { get; set; }

		public DateTime? PostingDateTime { get; set; }

		public DateTime? ValueDateTime { get; set; }

		public DateTime? ExecutionDateTime { get; set; }

		public decimal Amount { get; set; }

		public string Currency { get; set; }

		public string Reference { get; set; }

		public string MerchantName { get; set; }

		public string MerchantCategoryCode { get; set; }

		public string BillerCode { get; set; }

		public string BillerName { get; set; }

		public string Crn { get; set; }

		public string ApcaNumber { get; set; }

		public string AccountId { get; set; }
	}
}
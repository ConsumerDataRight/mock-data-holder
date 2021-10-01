using System;

namespace CDR.DataHolder.Resource.API.Business.Models
{
    public class AccountTransactionModel
    {
		public string AccountId { get; set; }

		public string TransactionId { get; set; }

		public bool IsDetailAvailable { get; set; }

		public string Type { get; set; }

		public string Status { get; set; }

		public string Description { get; set; }

		public DateTime? PostingDateTime { get; set; }

		public DateTime? ValueDateTime { get; set; }

		public DateTime? ExecutionDateTime { get; set; }

		public string Amount { get; set; }

		public string Currency { get; set; }

		public string Reference { get; set; }

		public string MerchantName { get; set; }

		public string MerchantCategoryCode { get; set; }

		public string BillerCode { get; set; }

		public string BillerName { get; set; }

		public string Crn { get; set; }

		public string ApcaNumber { get; set; }
	}
}

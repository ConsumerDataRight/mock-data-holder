using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CDR.DataHolder.Repository.Entities
{
	public class Transaction
	{
		[Key, Required]
		public string TransactionId { get; set; }

		public string TransactionType { get; set; }

		public string Status { get; set; }

		[Required, MaxLength(100)]
		public string Description { get; set; }

		public DateTime? PostingDateTime { get; set; }

		public DateTime? ValueDateTime { get; set; }

		public DateTime? ExecutionDateTime { get; set; }

		[Required]
		public decimal Amount { get; set; }

		[MaxLength(3)]
		public string Currency { get; set; }

		[Required, MaxLength(100)]
		public string Reference { get; set; }

		[MaxLength(200)]
		public string MerchantName { get; set; }

		[MaxLength(50)]
		public string MerchantCategoryCode { get; set; }

		[MaxLength(50)]
		public string BillerCode { get; set; }

		[MaxLength(100)]
		public string BillerName { get; set; }

		[MaxLength(100)]
		public string Crn { get; set; }

		[MaxLength(6)]
		public string ApcaNumber { get; set; }

		public string AccountId { get; set; }
		public virtual Account Account { get; set; }
	}
}

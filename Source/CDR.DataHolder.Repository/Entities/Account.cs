using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CDR.DataHolder.Domain.ValueObjects;

namespace CDR.DataHolder.Repository.Entities
{
	public class Account
	{
		[Key, MaxLength(100)]
		public string AccountId { get; set; }
		
		public DateTime? CreationDate { get; set; }

		[Required, MaxLength(100)]
		public string DisplayName { get; set; }

		[Required, MaxLength(50)]
		public string NickName { get; set; }

		public string OpenStatus { get; set; }

		public Guid CustomerId { get; set; }
		public virtual Customer Customer { get; set; }

		[Required, MaxLength(100)]
		public string MaskedName { get; set; }

		public string ProductCategory { get; set; }

		[Required, MaxLength(200)]
		public string ProductName { get; set; }

		public virtual ICollection<Transaction> Transactions { get; set; }

	}
}

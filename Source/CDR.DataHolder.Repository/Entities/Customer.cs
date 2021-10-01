using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace CDR.DataHolder.Repository.Entities
{
	public class Customer
	{
		public Customer()
		{
			this.CustomerId = new Guid();
		}

		[Key]
		public Guid CustomerId { get; set; }
		[Required, MaxLength(8)]
		public string LoginId { get; set; }

		public string CustomerUType { get; set; }
		
		public Guid? PersonId { get; set; }
		public virtual Person Person { get; set; }
		
		public Guid? OrganisationId { get; set; }
		public virtual Organisation Organisation { get; set; }

		public virtual ICollection<Account> Accounts { get; set; }
	}
}

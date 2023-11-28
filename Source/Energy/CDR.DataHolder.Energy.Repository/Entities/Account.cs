using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace CDR.DataHolder.Energy.Repository.Entities
{
    public class Account : Shared.Repository.Entities.Account
	{
        [MaxLength(100)]
        public string? DisplayName { get; set; }

        [MaxLength(100)]
		public string? AccountNumber { get; set; }
		public virtual Customer Customer { get; set; } = new Customer();
		public virtual ICollection<AccountPlan>? AccountPlans { get; set; }
		public virtual ICollection<AccountConcession>? AccountConcessions { get; set; }
	}
}

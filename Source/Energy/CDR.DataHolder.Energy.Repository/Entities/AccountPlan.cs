using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace CDR.DataHolder.Energy.Repository.Entities
{
    public class AccountPlan
    {
        public AccountPlan()
        {
            AccountPlanId = Guid.NewGuid().ToString();
            AccountId = Guid.NewGuid().ToString();
            Account = new Account() { AccountId = AccountId };
            PlanId = Guid.NewGuid().ToString();
            Plan = new Plan() { PlanId = PlanId };
        }

        [Key]
        [MaxLength(100)]
        public string AccountPlanId { get; set; }

        [Required]
        public string AccountId { get; set; }

        public virtual Account Account { get; set; }

        [Required]
        public string PlanId { get; set; }

        public virtual Plan Plan { get; set; }

        [MaxLength(100)]
        public string? Nickname { get; set; }

        public virtual ICollection<ServicePoint>? ServicePoints { get; set; }

        // configure 1-1 relationship
        public virtual PlanOverview? PlanOverview { get; set; }
    }
}

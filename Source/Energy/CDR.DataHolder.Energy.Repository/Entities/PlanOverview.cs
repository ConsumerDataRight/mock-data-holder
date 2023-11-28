using System;
using System.ComponentModel.DataAnnotations;

namespace CDR.DataHolder.Energy.Repository.Entities
{
	public class PlanOverview
	{
		[Key, MaxLength(100)]
		public string PlanOverviewId { get; set; } = string.Empty;

		public string? DisplayName { get; set; }
		public DateTime StartDate { get; set; }
		public DateTime? EndDate { get; set; }

		public string? AccountPlanId { get; set; }
		public virtual AccountPlan? AccountPlan { get; set; }
	}
}
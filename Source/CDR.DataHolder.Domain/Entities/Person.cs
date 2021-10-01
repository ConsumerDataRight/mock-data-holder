using System;

namespace CDR.DataHolder.Domain.Entities
{
	public class Person : Customer
	{
		private DateTime? lastUpdateTime;

		public Guid PersonId { get; set; }
		public string FirstName { get; set; }
		public string LastName { get; set; }
		public string[] MiddleNames { get; set; }
		public string Prefix { get; set; }
		public string Suffix { get; set; }
		public string OccupationCode { get; set; }
		public string OccupationCodeVersion { get; set; }
		public DateTime? LastUpdateTime { get => lastUpdateTime == null ? lastUpdateTime : lastUpdateTime.Value.ToUniversalTime(); set => lastUpdateTime = value; }
	}
}

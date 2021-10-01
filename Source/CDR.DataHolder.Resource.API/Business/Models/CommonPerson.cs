using System;

namespace CDR.DataHolder.Resource.API.Business.Models
{
	public class CommonPerson
	{
		public DateTime? LastUpdateTime { get; set; }
		public string FirstName { get; set; }
		public string LastName { get; set; }
		public string[] MiddleNames { get; set; }
		public string Prefix { get; set; }
		public string Suffix { get; set; }
		public string OccupationCode { get; set; }
		public string OccupationCodeVersion { get; set; }
	}
}

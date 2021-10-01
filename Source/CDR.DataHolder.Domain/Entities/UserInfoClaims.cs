using System;

namespace CDR.DataHolder.Domain.Entities
{
    public class UserInfoClaims
	{
		public string GivenName { get; set; }
		public string FamilyName { get; set; }
		public string Name { get; set; }
		public string Audience { get; set; }
		public DateTime? LastUpdated { get; set; }
	}
}

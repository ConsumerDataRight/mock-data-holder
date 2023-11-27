using System;

namespace CDR.DataHolder.Shared.Domain.Entities
{
    public class Account
	{
		public string AccountId { get; set; } = string.Empty;
		public DateTime? CreationDate { get; set; } = DateTime.MinValue;
		public string DisplayName { get; set; } = string.Empty;
		public string? OpenStatus { get; set; }
	}
}
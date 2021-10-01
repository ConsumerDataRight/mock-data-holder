using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace CDR.DataHolder.IdentityServer.Models.UI
{
	public class UserAuthViewModel
	{
        [Required]
        public string CustomerId { get; set; }
		[Required] 
		public string Otp { get; set; }
		public string ValidOtp { get; set; }
		public bool ShowOtp { get; set; }
		public string ReturnUrl { get; set; }
		public bool EnableLogin { get; internal set; }
		public string ClientName { get; internal set; }

		public static class ButtonActions
		{
			public const string Page1 = "page1";
			public const string Page2 = "page2";
			public const string Authenticate = "auth";
			public const string Cancel = "cancel";
		}

		public void ClearInputs()
		{
			this.CustomerId = string.Empty;
			this.Otp = string.Empty;
		}
	}
}

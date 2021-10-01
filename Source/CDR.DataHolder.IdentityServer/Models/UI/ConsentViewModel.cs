// Copyright (c) Brock Allen & Dominick Baier. All rights reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.


using System;
using System.Collections.Generic;
using System.Linq;

namespace CDR.DataHolder.IdentityServer.Models.UI
{
    public class ConsentViewModel : ConsentInputModel
    {
		public ConsentViewModel()
		{
            Accounts = new List<AccountModel>();
        }

        public string ClientName { get; set; }
        public string ClientUrl { get; set; }
        public string ClientLogoUrl { get; set; }
		public bool AllowRememberConsent { get; set; }
		public IEnumerable<AccountModel> Accounts { get; internal set; }
		public IEnumerable<AccountModel> InvalidAccounts { get; internal set; }

		public TimeSpan ConsentLifeTimeSpan { get; internal set; }

		public class ActionTypes
        {
            public const string Cancel = "cancel";
            public const string Page2 = "page2";
            public const string Page1 = "page1";
            public const string Consent = "consent";
        }
    }
}

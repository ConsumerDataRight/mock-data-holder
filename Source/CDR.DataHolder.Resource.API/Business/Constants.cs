namespace CDR.DataHolder.Resource.API.Business
{
	public static class Constants
	{
		public static class TokenClaimTypes
		{
			public const string AccountId = "account_id";
			public const string SectorIdentifier = "sector_identifier";
			public const string SoftwareId = "software_id";
		}

		public static class ResourceEndPoints
		{
			public const string GetAccounts = "/banking/accounts";
		}

		public static class UnauthorisedErrors
		{
			public const string InvalidToken = "invalid_token";
			public const string ErrorMessage = $@"{{
                            ""errors"": [
                                {{
                                ""code"": ""401"",
                                ""title"": ""Unauthorized"",
                                ""detail"": ""ErrorDetail"",
                                ""meta"": {{}}
                                }}
                            ]
                        }}";
			public const string ErrorMessageDetailReplace = "ErrorDetail";
		}
	}
}

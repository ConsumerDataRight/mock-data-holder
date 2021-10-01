namespace CDR.DataHolder.Resource.API.Business
{
	public class Constants
	{
		public static class TokenClaimTypes
		{
			public static string AccontId = "account_id";
			public static string SectorIdentifier = "sector_identifier";
			public static string SoftwareId = "software_id";
		}

		public static class ResourceEndPoints
		{
			public static string GetAccounts = "/banking/accounts";
		}

		public static class UnauthorisedErrors
		{
			public static string InvalidToken = "invalid_token";
			public static string ErrorMessage = $@"{{
                            ""errors"": [
                                {{
                                ""code"": ""401"",
                                ""title"": ""Unauthorized"",
                                ""detail"": ""ErrorDetail"",
                                ""meta"": {{}}
                                }}
                            ]
                        }}";
			public static string ErrorMessageDetailReplace = "ErrorDetail";
		}
	}
}

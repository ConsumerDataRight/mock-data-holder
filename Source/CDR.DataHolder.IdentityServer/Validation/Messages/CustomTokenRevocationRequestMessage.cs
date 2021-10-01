namespace CDR.DataHolder.IdentityServer.Validation.Messages
{
    public static class CustomTokenRevocationRequestMessage
    {
        public const string NoTokenFoundInRequest = "No token found in request";

        public const string InvalidTokenTypeHintAccessToken = "Invalid token type hint: access_token";
    }
}

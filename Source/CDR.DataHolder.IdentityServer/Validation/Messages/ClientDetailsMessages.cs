namespace CDR.DataHolder.IdentityServer.Validation.Messages
{
    public static class ClientDetailsMessages
    {
        public const string ClientAssertionMissing = "Client assertion token not provided.";
        public const string InvalidClientAssertion = "Client assertion token is not valid.";
        public const string ClientAssertionTooLong = "Client assertion token exceeds maximum length.";
        public const string TrustedKeysEmpty = "There are no keys available to validate client assertion.";
        public const string TrustedKeysMissing = "Could not load secrets.";
        public const string MissingClientId = "Client id not provided.";
        public const string InvalidClientAssertionType = "Invalid client assertion type.";
        public const string JtiIsMissing = "The 'jti' parameter is null or whitespace.";
        public const string JtiAlreadyUsed = "The 'jti' has previously been used.";
        public const string SubIsMissing = "The 'sub' parameter is null or whitespace.";
        public const string SubTooLong = "Client assertion token sub claim (client_id) exceeds maximum length.";
        public const string InvalidSub = "Subject validation failed.";
        public const string TokenExpired = "Lifetime validation failed. The token is expired.";
        public const string InvalidAud = "Audience validation failed.";
        public const string InvalidNbf = "Token 'nbf' claim is > 'exp' claim.";
        public const string InvalidSignature = "Signature validation failed. Unable to match key.";
        public const string ExpIsMissing = "Lifetime validation failed. The token is missing an Expiration Time.";
        public const string InvalidValidFrom = "Lifetime validation failed. The token is not yet valid.";
        public const string TokenReplayed = "The securityToken has previously been validated.";
        public const string InvalidIss = "Unable to validate issuer. The 'issuer' parameter is null, whitespace or invalid.";
        public const string ClientAssertionParseError = "Invalid client_assertion";
        public const string InvalidAlg = "Invalid Alg header";
    }
}
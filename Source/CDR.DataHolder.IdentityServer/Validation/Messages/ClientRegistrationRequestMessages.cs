namespace CDR.DataHolder.IdentityServer.Validation.Messages
{
    public static class ClientRegistrationRequestMessages
    {
        public const string DuplicateRegistration = "Duplicate registration.";
        public const string SSAJwtFailedValidation = "SSA Jwt failed validation.";
        public const string RequestJwtFailedValidation = "Request Jwt failed validation.";
        public const string SoftwareIdNotEqualIssuer = "Software_id is not equal to the value of issuer.";
        public const string RedirectUrisNotMatch = "RedirectUris do not match.";
        public const string AudienceUriNotMatch = "AudienceUri does not match.";
        public const string JwksUriEndpointNotFound = "JwksUri failed with NotFound.";
        public const string JwksEndpointDidNotReturnSuccess = "Call to JwksUri did not return success.";
        public const string JwksEndpointDidNotReturnEncryptionJwk = "Call to JwksUri did not return encryption jwk.";
        public const string JwksUriEndpointReturndInvalidJwk = "Call to JwksUri returned invalid Jwk.";
    }
}

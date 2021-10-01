namespace CDR.DataHolder.IdentityServer.Validation.Messages
{
    public static class ClientRevocationRequestMessages
    {
        public const string MissingTokenParameter = "'token' is required.";

        public const string MissingClientDetails = "Missing client details.";

        public const string MissingMtlsCredentials = "Missing MTLS credentials.";
    }
}

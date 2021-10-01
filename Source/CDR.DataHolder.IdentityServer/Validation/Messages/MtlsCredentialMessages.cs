namespace CDR.DataHolder.IdentityServer.Validation.Messages
{
    public static class MtlsCredentialMessages
    {
        public const string ClientCertificateCommonNameMissing = "Request header X-TlsClientCertCN not found.";
        public const string ClientCertificateThumbprintMissing = "Request header X-TlsClientCertThumbprint not found.";
    }
}
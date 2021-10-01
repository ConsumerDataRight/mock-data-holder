namespace CDR.DataHolder.IdentityServer.Models
{
    public class MtlsCredential
    {
        public string ClientId { get; set; }

        public string CertificateThumbprint { get; set; }

        public string CertificateCommonName { get; set; }
    }
}
namespace CDR.DataHolder.IdentityServer.Models
{
    public class RepositoryClient
    {
        public string Id { get; set; }

        public string Name { get; set; }

        public string JwksUri { get; set; }

        public RepositoryClientCertificate X509Certificate { get; set; }

        public string[] RedirectUris { get; set; }

    }

    public class RepositoryClientCertificate
    {
        public string CommonName { get; set; }

        public string Thumbprint { get; set; }
    }
}
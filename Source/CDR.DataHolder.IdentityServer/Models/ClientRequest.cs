namespace CDR.DataHolder.IdentityServer.Models
{
    public class ClientRequest
    {
        public MtlsCredential MtlsCredential { get; set; }

        public ClientDetails ClientDetails { get; set; }
    }
}

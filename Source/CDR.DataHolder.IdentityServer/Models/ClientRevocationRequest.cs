namespace CDR.DataHolder.IdentityServer.Models
{
    public class ClientRevocationRequest : ClientRequest
    {
        public string Token { get; set; }

        public string TokenTypeHint { get; set; }
    }
}

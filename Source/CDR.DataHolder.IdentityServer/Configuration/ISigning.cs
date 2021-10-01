namespace CDR.DataHolder.IdentityServer.Configuration
{
    public interface ISigning
    {
        string ServerSigningKey { get; set; }

        string ServerSigningKeyPassword { get; set; }

        string ClientSigningKey { get; set; }

        string ClientSigningJwks { get; set; }

        string RegisterJwks { get; set; }
    }
}

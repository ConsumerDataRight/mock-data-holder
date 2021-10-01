namespace CDR.DataHolder.IdentityServer.Configuration
{
    public interface IRegistration
    {
        string AudienceUri { get; set; }

        string AuthorizationCodeLifetime { get; set; }

        string RefreshTokenLifetime { get; set; }
    }
}

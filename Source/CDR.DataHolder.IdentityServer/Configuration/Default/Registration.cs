namespace CDR.DataHolder.IdentityServer.Configuration
{
    public class Registration : IRegistration
    {
        public string AudienceUri { get; set; }
        public string AuthorizationCodeLifetime { get; set; }
        public string RefreshTokenLifetime { get; set; }
        public string SystemSalt { get; set; }
    }
}
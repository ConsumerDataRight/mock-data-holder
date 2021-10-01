using System;
using Microsoft.Extensions.Configuration;

namespace CDR.DataHolder.IdentityServer.Configuration
{
    public class ConfigurationSettings : IConfigurationSettings
    {
        public ConfigurationSettings(IConfiguration configuration)
        {
            SHA256SaltForClientSubValue = configuration.GetValue<string>("SHA256SaltForClientSubValue", null);
            IssuerUri = configuration.GetValue<string>("IssuerUri", null);
            PublicOrigin = configuration.GetValue<string>("PublicOrigin", null);
            AccessTokenLifetimeSeconds = configuration.GetValue("AccessTokenLifetimeSeconds", 300);
            ParRequestUriExpirySeconds = configuration.GetValue("ParRequestUriExpirySeconds", 90);
            JwksUri = configuration.GetValue<Uri>("JwksUri", null);
            RegisterSsaJwksUri = configuration.GetValue<string>("Register:SsaJwksUri", null);
            Registration = configuration.GetSection("Registration").Get<Registration>();
            KeyStore = configuration.GetSection("KeyStore").Get<KeyStore>();
        }

        public string SHA256SaltForClientSubValue { get; set; }

        public string IssuerUri { get; set; }

        public string PublicOrigin { get; set; }

        public IKeyStore KeyStore { get; set; }

        public IRegistration Registration { get; set; }

        public Uri JwksUri { get; set; }

        public string RegisterSsaJwksUri { get; set; }
        
        public int AccessTokenLifetimeSeconds { get; set; }

        public int ParRequestUriExpirySeconds { get; set; }
    }
}
using System;

namespace CDR.DataHolder.IdentityServer.Configuration
{
    public interface IConfigurationSettings
    {
        IKeyStore KeyStore { get; set; }

        string SHA256SaltForClientSubValue { get; set; }

        string IssuerUri { get; set; }

        string PublicOrigin { get; set; }

        IRegistration Registration { get; set; }

        Uri JwksUri { get; set; }

        string RegisterSsaJwksUri { get; set; }

        int AccessTokenLifetimeSeconds { get; set; }

        int ParRequestUriExpirySeconds { get; set; }
    }
}
namespace CDR.DataHolder.IdentityServer.Configuration
{
    public static class Constants
    {
        public static class SecretTypes
        {
            public const string JwksUrl = "JWKSURL";
        }

        public static class ParsedSecretTypes
        {
            public const string CdrSecret = "CdrSecret";
        }

        public static class ConfigurationKeys
        {
            public const string IssuerUri = "IssuerUri";
            public const string JwksUri = "JwksUri";
            public const string TokenUri = "TokenUri";
            public const string ConfigUri = "ConfigUri";
            public const string AuthorizeUri = "AuthorizeUri";
            public const string IntrospectionUri = "IntrospectionUri";
            public const string UserinfoUri = "UserinfoUri";
            public const string RegisterUri = "RegisterUri";
            public const string ParUri = "ParUri";
            public const string RevocationUri = "RevocationUri";
            public const string ArrangementRevocationUri = "ArrangementRevocationUri";
        }

        public static class DiscoveryOverrideKeys
        {
            public const string JwksUri = "jwks_uri_override";
            public const string TokenEndpoint = "token_endpoint_override";
        }
    }
    
}

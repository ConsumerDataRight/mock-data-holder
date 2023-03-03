namespace CDR.DataHolder.API.Infrastructure
{
    public static class Constants
    {
        public static class CustomHeaders
        {
            public const string ClientCertThumbprintHeaderKey = "X-TlsClientCertThumbprint";
            public const string ClientCertClientNameHeaderKey = "X-TlsClientCertCN";
            public const string ApiVersionHeaderKey = "x-v";
            public const string ApiMinVersionHeaderKey = "x-min-v";
        }

        public static class ApiScopes
        {
            public static class Banking
            {
                public const string AccountsBasicRead = "bank:accounts.basic:read";
                public const string AccountsDetailRead = "bank:accounts.detail:read";
                public const string PayeesRead = "bank:payees:read";
                public const string RegularPaymentsRead = "bank:regular_payments:read";
                public const string TransactionsRead = "bank:transactions:read";
            }

            public static class Common
            {
                public const string CustomerBasicRead = "common:customer.basic:read";
                public const string CustomerDetailRead = "common:customer.detail:read";
            }
        }

        public static class CdrScopes
        {
            public const string Registration = "cdr:registration";
            public const string MetricsBasicRead = "admin:metrics.basic:read";
            public const string MetadataUpdate = "admin:metadata:update";
        }

        public static class StandardScopes
        {
            //
            // Summary:
            //     REQUIRED. Informs the Authorization Server that the Client is making an OpenID
            //     Connect request. If the openid scope value is not present, the behavior is entirely
            //     unspecified.
            public const string OpenId = "openid";
            //
            // Summary:
            //     OPTIONAL. This scope value requests access to the End-User's default profile
            //     Claims, which are: name, family_name, given_name, middle_name, nickname, preferred_username,
            //     profile, picture, website, gender, birthdate, zoneinfo, locale, and updated_at.
            public const string Profile = "profile";
            //
            // Summary:
            //     This scope value MUST NOT be used with the OpenID Connect Implicit Client Implementer's
            //     Guide 1.0. See the OpenID Connect Basic Client Implementer's Guide 1.0 (http://openid.net/specs/openid-connect-implicit-1_0.html#OpenID.Basic)
            //     for its usage in that subset of OpenID Connect.
            public const string OfflineAccess = "offline_access";
        }

    }
}

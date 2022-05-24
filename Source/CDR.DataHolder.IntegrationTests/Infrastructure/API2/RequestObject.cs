using System;
using System.Collections.Generic;

#nullable enable

namespace CDR.DataHolder.IntegrationTests.Infrastructure.API2
{
    public class RequestObject
    {
        public int? SharingDuration { get; init; } = 7776000;
        public long? IssuedAt { get; init; } = null;
        public long? Expiry { get; init; } = null;
        public long? NotBefore { get; init; } = null;
        public string? ClientId { get; init; } = BaseTest.SOFTWAREPRODUCT_ID.ToLower();
        public string? RedirectUri { get; init; } = BaseTest.SOFTWAREPRODUCT_REDIRECT_URI_FOR_INTEGRATION_TESTS;
        public string? Scope { get; init; } = BaseTest.SCOPE;
        public string? Aud { get; init; } = BaseTest.DH_TLS_IDENTITYSERVER_BASE_URL;
        public string? ResponseMode { get; init; } = "fragment";
        public string? JwtCertificateFilename { get; init; } = null;
        public string? JwtCertificatePassword { get; init; } = null;
        public string? CdrArrangementId { get; init; } = null;
        public string? CodeChallenge { get; init; } = null;
        public string? CodeChallengeMethod { get; init; } = "S256";

        public string Get()
        {

            var requestObject = new Dictionary<string, object>
            {
                { "iss", ClientId ?? BaseTest.SOFTWAREPRODUCT_ID.ToLower() },
                { "iat", IssuedAt ?? new DateTimeOffset(DateTime.Now).ToUnixTimeSeconds() },
                { "jti", Guid.NewGuid().ToString().Replace("-", string.Empty) },
                { "response_type", "code id_token"},
                { "response_mode", ResponseMode},
                { "client_id", ClientId ?? BaseTest.SOFTWAREPRODUCT_ID.ToLower() },
                { "redirect_uri", BaseTest.SubstituteConstant(RedirectUri) },
                { "code_challenge", CodeChallenge },
                { "code_challenge_method", CodeChallengeMethod },
                { "state", Guid.NewGuid().ToString() },
                { "nonce", Guid.NewGuid().ToString() },
                { "claims", new {
                    sharing_duration = $"{SharingDuration}",
                    cdr_arrangement_id = CdrArrangementId,
                    id_token = new {
                        acr = new {
                            essential = true,
                            values = new string[] { "urn:cds.au:cdr:2" }
                        }
                    },
                }}
            };

            if (NotBefore != null)
            {
                requestObject.Add("nbf", NotBefore.Value);
            }

            if (Expiry != null)
            {
                requestObject.Add("exp", Expiry.Value);
            }

            if (Scope != null)
            {
                requestObject.Add("scope", Scope);
            }
            if (Aud != null)
            {
                requestObject.Add("aud", Aud);
            }

            var jwt = JWT2.CreateJWT(JwtCertificateFilename ?? BaseTest.JWT_CERTIFICATE_FILENAME, JwtCertificatePassword ?? BaseTest.JWT_CERTIFICATE_PASSWORD, requestObject);

            return jwt;
        }
    }
}

using System;
using System.Collections.Generic;

#nullable enable

namespace CDR.DataHolder.IntegrationTests.Infrastructure.API2
{
    public class RequestObject
    {
        public string? Scope { get; init; } = BaseTest.SCOPE;
        public string? Aud { get; init; } = BaseTest.DH_TLS_IDENTITYSERVER_BASE_URL;
        public string? CdrArrangementId { get; init; } = null;

        public string Get()
        {
            var iat = new DateTimeOffset(DateTime.Now).ToUnixTimeSeconds();

            var subject = new Dictionary<string, object>
            {
                { "iss", BaseTest.SOFTWAREPRODUCT_ID.ToLower() },
                // { "sub", BaseTest.SOFTWAREPRODUCT_ID.ToLower() },
                { "iat", iat },
                { "exp", iat + 14400 },
                { "jti", Guid.NewGuid().ToString().Replace("-", string.Empty) },
                { "response_type", "code id_token"},
                // { "response_mode", "form_post" },
                { "client_id", BaseTest.SOFTWAREPRODUCT_ID.ToLower() },
                // { "redirect_uri", BaseTest.SOFTWAREPRODUCT_REDIRECT_URI },
                { "redirect_uri", BaseTest.SOFTWAREPRODUCT_REDIRECT_URI_FOR_INTEGRATION_TESTS },
                { "state", Guid.NewGuid().ToString() },
                { "nonce", Guid.NewGuid().ToString() },
                { "claims", new {
                    sharing_duration = "7776000",
                    cdr_arrangement_id = CdrArrangementId,
                    id_token = new {
                        acr = new {
                            essential = true,
                            values = new string[] { "urn:cds.au:cdr:2" }
                        }
                    },
                    // userinfo = new {
                    //      given_name = "Lily",
                    //      family_name = "Wang",
                    // }
                }}
            };

            if (Scope != null)
            {
                subject.Add("scope", Scope);
            }
            if (Aud != null)
            {
                subject.Add("aud", Aud);
            }

            // var jwt = JWT2.CreateJWT(BaseTest.CERTIFICATE_FILENAME, BaseTest.CERTIFICATE_PASSWORD, subject);
            var jwt = JWT2.CreateJWT(BaseTest.JWT_CERTIFICATE_FILENAME, BaseTest.JWT_CERTIFICATE_PASSWORD, subject);

            return jwt;
        }
    }
}

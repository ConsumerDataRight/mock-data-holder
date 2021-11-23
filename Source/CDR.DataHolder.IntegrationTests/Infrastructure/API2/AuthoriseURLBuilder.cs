using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.WebUtilities;

#nullable enable

namespace CDR.DataHolder.IntegrationTests.Infrastructure.API2
{
    /// <summary>
    /// Url builder for Authorise endpoint
    /// </summary>
    public class AuthoriseURLBuilder
    {
        public string ClientId { get; init; } = BaseTest.SOFTWAREPRODUCT_ID.ToLower();
        // public string RedirectURI { get; init; } = BaseTest.SOFTWAREPRODUCT_REDIRECT_URI;
        public string RedirectURI { get; init; } = BaseTest.SOFTWAREPRODUCT_REDIRECT_URI_FOR_INTEGRATION_TESTS;
        // public string CertificiateFilename { get; init; } = BaseTest.CERTIFICATE_FILENAME;
        // public string CertificiatePassword { get; init; } = BaseTest.CERTIFICATE_PASSWORD;
        public string JWT_CertificateFilename { get; init; } = BaseTest.JWT_CERTIFICATE_FILENAME;
        public string JWT_CertificatePassword { get; init; } = BaseTest.JWT_CERTIFICATE_PASSWORD;
        public string Scope { get; init; } = BaseTest.SCOPE;
        public string ResponseType { get; init; } = "code id_token";
        public string? Request { get; init; } = null; // use this as the request, rather than build request
        public string? RequestUri { get; init; } = null;
        // public bool SignJWT { get; init; } = true; // sign the jwt?


        /// <summary>
        /// Lifetime (in seconds) of the access token. It has to be less than 60 mins
        /// </summary>
        public int TokenLifetime { get; init; } = 3600;

        /// <summary>
        /// Lifetime (in seconds) of the CDR arrangement.
        /// 7776000 = 90 days
        /// </summary>
        public int SharingDuration { get; init; } = 7776000;

        public string URL
        {
            get
            {
                var queryString = new Dictionary<string, string?>
                {
                    { "client_id", ClientId },
                    { "response_type", ResponseType },
                };

                if (RequestUri != null)
                {
                    queryString.Add("request_uri", RequestUri);
                }
                else
                {
                    queryString.Add("request", Request ?? CreateRequest());
                }

                var url = QueryHelpers.AddQueryString("https://localhost:8001/connect/authorize", queryString);
                // var url = QueryHelpers.AddQueryString("https://localhost:8002/connect/authorize", queryString);

                return url;
            }
        }

        private string CreateRequest()
        {
            var iat = new DateTimeOffset(DateTime.Now).ToUnixTimeSeconds();

            var subject = new Dictionary<string, object>
                {
                    { "iss", ClientId },
                    { "iat", iat },
                    { "exp", iat + TokenLifetime },
                    { "jti", Guid.NewGuid().ToString().Replace("-", string.Empty) },
                    { "aud", "https://localhost:8001" },
                    { "response_type", ResponseType },
                    { "client_id", ClientId },
                    { "redirect_uri", RedirectURI },
                    { "scope", Scope },
                    { "state", "foo" },  
                    { "nonce", "foo" },  
                    { "claims", new {
                        sharing_duration = SharingDuration.ToString(),
                        id_token = new {
                            acr = new {
                                essential = true,
                                values = new string[] { "urn:cds.au:cdr:2" }
                            }
                        }
                    }}
                };

            // if (RequestUri != null)
            // {
            //     subject.Add("request_uri", RequestUri);
            // }

            return JWT2.CreateJWT(JWT_CertificateFilename, JWT_CertificatePassword, subject);
        }
    }
}
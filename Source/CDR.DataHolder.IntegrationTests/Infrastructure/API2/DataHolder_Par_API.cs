using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using CDR.DataHolder.IntegrationTests.Extensions;
using Newtonsoft.Json;

#nullable enable

namespace CDR.DataHolder.IntegrationTests.Infrastructure.API2
{
    public class DataHolder_Par_API
    {
        public class Response
        {
            [JsonProperty("request_uri")]
            public string? RequestURI { get; set; }

            [JsonProperty("expires_in")]
            public int? ExpiresIn { get; set; }
        };

        static public async Task<HttpResponseMessage> SendRequest(
             string? clientId = BaseTest.SOFTWAREPRODUCT_ID,
             string? clientAssertionType = BaseTest.CLIENTASSERTIONTYPE,
             string? scope = BaseTest.SCOPE,
             int? sharingDuration = BaseTest.SHARING_DURATION,
             string? aud = null,
             int nbfOffsetSeconds = 0,
             int expOffsetSeconds = 0,
             bool addRequestObject = true,
             bool addNotBeforeClaim = true,
             bool addExpiryClaim = true,
             string? cdrArrangementId = null,
             string? redirectUri = BaseTest.SOFTWAREPRODUCT_REDIRECT_URI_FOR_INTEGRATION_TESTS,
             string? clientAssertion = null,

             string? codeVerifier = BaseTest.FAPI_PHASE2_CODEVERIFIER,
             string? codeChallengeMethod = BaseTest.FAPI_PHASE2_CODECHALLENGEMETHOD,

             string? requestUri = null,
             string? responseMode = "fragment",
             string? certificateFilename = BaseTest.CERTIFICATE_FILENAME,
             string? certificatePassword = BaseTest.CERTIFICATE_PASSWORD,
             string? jwtCertificateForClientAssertionFilename = BaseTest.JWT_CERTIFICATE_FILENAME,
             string? jwtCertificateForClientAssertionPassword = BaseTest.JWT_CERTIFICATE_PASSWORD,
             string? jwtCertificateForRequestObjectFilename = BaseTest.JWT_CERTIFICATE_FILENAME,
             string? jwtCertificateForRequestObjectPassword = BaseTest.JWT_CERTIFICATE_PASSWORD)
        {
            redirectUri = BaseTest.SubstituteConstant(redirectUri);

            if (clientId == BaseTest.SOFTWAREPRODUCT_ID) // FIXME - MJS - messy workaround, fix this?
            {
                clientId = BaseTest.GetClientId(BaseTest.SOFTWAREPRODUCT_ID);
            }

            var issuer = BaseTest.DH_TLS_AUTHSERVER_BASE_URL;

            // var parUrl = $"{BaseTest.DH_TLS_AUTHSERVER_BASE_URL}/connect/par";
            var parUrl = $"{BaseTest.CDRAUTHSERVER_SECUREBASEURI}/connect/par";

            var formFields = new List<KeyValuePair<string?, string?>>();

            if (clientAssertionType != null)
            {
                formFields.Add(new KeyValuePair<string?, string?>("client_assertion_type", clientAssertionType));
            }

            if (requestUri != null)
            {
                formFields.Add(new KeyValuePair<string?, string?>("request_uri", requestUri));
            }

            formFields.Add(new KeyValuePair<string?, string?>("client_assertion", clientAssertion ??
                new PrivateKeyJwt2()
                {
                    CertificateFilename = jwtCertificateForClientAssertionFilename,
                    CertificatePassword = jwtCertificateForClientAssertionPassword,
                    Issuer = clientId ?? throw new NullReferenceException(nameof(clientId)),
                    Audience = aud ?? issuer
                }.Generate()
            ));

            if (addRequestObject)
            {
                var iat = new DateTimeOffset(DateTime.Now).ToUnixTimeSeconds();
                var request = new RequestObject()
                {
                    Aud = aud ?? BaseTest.DH_TLS_AUTHSERVER_BASE_URL,
                    IssuedAt = iat,
                    NotBefore = addNotBeforeClaim ? iat + nbfOffsetSeconds : null,
                    Expiry = addExpiryClaim ? iat + expOffsetSeconds + 600 : null,
                    ClientId = clientId,
                    RedirectUri = redirectUri,
                    Scope = scope,
                    CdrArrangementId = cdrArrangementId,
                    SharingDuration = sharingDuration,
                    ResponseMode = responseMode,
                    JwtCertificateFilename = jwtCertificateForRequestObjectFilename,
                    JwtCertificatePassword = jwtCertificateForRequestObjectPassword,

                    CodeChallenge = codeVerifier?.CreatePkceChallenge(), 
                    CodeChallengeMethod = codeChallengeMethod 
                };

                formFields.Add(new KeyValuePair<string?, string?>("request", request.Get()));
            }

            var content = new FormUrlEncodedContent(formFields);

            using var clientHandler = new HttpClientHandler();
            clientHandler.ServerCertificateCustomValidationCallback += (sender, cert, chain, sslPolicyErrors) => true;
            clientHandler.ClientCertificates.Add(new X509Certificate2(
                certificateFilename ?? throw new ArgumentNullException(nameof(certificateFilename)),
                certificatePassword,
                X509KeyStorageFlags.Exportable));

            using var client = new HttpClient(clientHandler);

            BaseTest.AttachHeadersForStandAlone(parUrl, content.Headers);

            var responseMessage = await client.PostAsync(parUrl, content);

            return responseMessage;
        }

        static public async Task<Response?> DeserializeResponse(HttpResponseMessage response)
        {
            var responseContent = await response.Content.ReadAsStringAsync();

            if (String.IsNullOrEmpty(responseContent))
            {
                return null;
            }

            return JsonConvert.DeserializeObject<Response>(responseContent);
        }
    }
}

using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using Newtonsoft.Json;

#nullable enable

namespace CDR.DataHolder.IntegrationTests.Infrastructure.API2
{
    public class DataHolder_Introspection_API
    {
        public class Response
        {
            [JsonProperty("active")]
            public bool? Active { get; set; }

            [JsonProperty("scope")]
            public string? Scope { get; set; }

            [JsonProperty("exp")]
            public int? Exp { get; set; }

            [JsonProperty("cdr_arrangement_id")]
            public string? CdrArrangementId { get; set; }
        };

        static public async Task<HttpResponseMessage> SendRequest(
             string? grantType = "client_credentials",
             string? clientId = BaseTest.SOFTWAREPRODUCT_ID,
             string? clientAssertionType = BaseTest.CLIENTASSERTIONTYPE,
             string? clientAssertion = null,
             string? token = null, 
             string? tokenTypeHint = "refresh_token")
        {
            var URL = $"{BaseTest.DH_MTLS_GATEWAY_URL}/connect/introspect";

            var formFields = new List<KeyValuePair<string?, string?>>();
            if (grantType != null)
            {
                formFields.Add(new KeyValuePair<string?, string?>("grant_type", grantType));
            }
            if (clientId != null)
            {
                formFields.Add(new KeyValuePair<string?, string?>("client_id", clientId.ToLower()));
            }
            if (clientAssertionType != null)
            {
                formFields.Add(new KeyValuePair<string?, string?>("client_assertion_type", clientAssertionType));
            }
            formFields.Add(new KeyValuePair<string?, string?>("client_assertion", clientAssertion ?? 
                // new ClientAssertion { Aud = URL }.Get()
                new PrivateKeyJwt2() 
                {
                    CertificateFilename = BaseTest.JWT_CERTIFICATE_FILENAME,
                    CertificatePassword = BaseTest.JWT_CERTIFICATE_PASSWORD,
                    Issuer = BaseTest.SOFTWAREPRODUCT_ID.ToLower(),
                    Audience = URL
                }.Generate()
            ));
            if (token != null)
            {
                formFields.Add(new KeyValuePair<string?, string?>("token", token));
            }
            if (tokenTypeHint != null)
            {
                formFields.Add(new KeyValuePair<string?, string?>("token_type_hint", tokenTypeHint));
            }
            var content = new FormUrlEncodedContent(formFields);

            using var clientHandler = new HttpClientHandler();
            clientHandler.ServerCertificateCustomValidationCallback += (sender, cert, chain, sslPolicyErrors) => true;
            clientHandler.ClientCertificates.Add(new X509Certificate2(BaseTest.CERTIFICATE_FILENAME, BaseTest.CERTIFICATE_PASSWORD, X509KeyStorageFlags.Exportable));

            using var client = new HttpClient(clientHandler);

            var responseMessage = await client.PostAsync(URL, content);

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

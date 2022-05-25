using System.Collections.Generic;
using System.Net.Http;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;

#nullable enable

namespace CDR.DataHolder.IntegrationTests.Infrastructure.API2
{
    public class DataHolder_CDRArrangementRevocation_API
    {
        static public async Task<HttpResponseMessage> SendRequest(
             string? grantType = "client_credentials",
             string? clientId = BaseTest.SOFTWAREPRODUCT_ID,
             string? clientAssertionType = BaseTest.CLIENTASSERTIONTYPE,
             string? cdrArrangementId = null,
             string? clientAssertion = null,
             string? certificateFilename = null,
             string? certificatePassword = null,
             string? jwtCertificateFilename = BaseTest.JWT_CERTIFICATE_FILENAME,
             string? jwtCertificatePassword = BaseTest.JWT_CERTIFICATE_PASSWORD)
        {
            var URL = $"{BaseTest.DH_MTLS_GATEWAY_URL}/connect/arrangements/revoke";

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
            if (cdrArrangementId != null)
            {
                formFields.Add(new KeyValuePair<string?, string?>("cdr_arrangement_id", cdrArrangementId));
            }
            formFields.Add(new KeyValuePair<string?, string?>("client_assertion", clientAssertion ??
                new PrivateKeyJwt2() 
                {
                    CertificateFilename = jwtCertificateFilename,
                    CertificatePassword = jwtCertificatePassword,
                    Issuer = BaseTest.SOFTWAREPRODUCT_ID.ToLower(),
                    Audience = URL
                }.Generate()
            ));
            var content = new FormUrlEncodedContent(formFields);

            using var clientHandler = new HttpClientHandler();
            clientHandler.ServerCertificateCustomValidationCallback += (sender, cert, chain, sslPolicyErrors) => true;
            clientHandler.ClientCertificates.Add(new X509Certificate2(
                certificateFilename ?? BaseTest.CERTIFICATE_FILENAME,
                certificatePassword ?? BaseTest.CERTIFICATE_PASSWORD,
                X509KeyStorageFlags.Exportable));

            using var client = new HttpClient(clientHandler);

            var responseMessage = await client.PostAsync(URL, content);

            return responseMessage;
        }
    }
}

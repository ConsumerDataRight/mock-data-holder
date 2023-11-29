using System;
using System.Net;
using System.Security.Cryptography.X509Certificates;

namespace CDR.DataHolder.Shared.API.Infrastructure.Extensions
{
    public static class CertificateExtensions
    {
        public static string ConvertToEncodedBase64String(this X509Certificate cert)
        {
            var data = cert.Export(X509ContentType.Cert);

            // Convert the byte array to a Base64-encoded string
            string base64Cert = Convert.ToBase64String(data);

            // Encode the string
            return WebUtility.UrlEncode(base64Cert);

        }
    }
}

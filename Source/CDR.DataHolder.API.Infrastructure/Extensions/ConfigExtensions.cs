using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Net.Http;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;

namespace CDR.DataHolder.API.Infrastructure.Extensions
{
    public static class ConfigExtensions
    {
        /// <summary>
        /// Retrieves a TLS Certificate based on configuration values.
        /// Allows a TLS certificate to be overridden from a remote file.
        /// </summary>
        /// <param name="config">IConfiguration</param>
        /// <returns>
        /// Uses the following configuration values:
        /// TlsCertificate:Url
        /// TlsCertificate:Password
        /// </returns>
        public static X509Certificate2 GetTlsCertificateOverride(this IConfiguration config, ILogger logger)
        {
            var certUrl = config.GetValue<string>("TlsCertificateOverride:Url", null);
            var certPassword = config.GetValue<string>("TlsCertificateOverride:Password", null);

            logger.LogInformation("TlsCertificateOverride = {certUrl} {certPassLength}", certUrl, certPassword == null ? 0 : certPassword.Length);

            if (string.IsNullOrEmpty(certUrl) || string.IsNullOrEmpty(certPassword))
            {
                logger.LogInformation("TlsCertificateOverride override details not found.");
                return null;
            }

            var cert = new X509Certificate2(DownloadData(certUrl), certPassword, X509KeyStorageFlags.Exportable);
            logger.LogInformation("Downloaded certificate: {thumbprint}", cert.Thumbprint);
            return cert;
        }

        private static byte[] DownloadData(string url)
        {
            using (var http = new HttpClient())
            {
                byte[] result = null;
                Task.Run(async () => result = await http.GetByteArrayAsync(url)).Wait();
                return result;
            }
        }
    }
}

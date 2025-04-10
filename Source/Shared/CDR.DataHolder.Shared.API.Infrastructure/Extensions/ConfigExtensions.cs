﻿using Microsoft.Extensions.Configuration;
using Serilog;
using System;
using System.Net.Http;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;

namespace CDR.DataHolder.Shared.API.Infrastructure.Extensions
{
    public static class ConfigExtensions
    {
        /// <summary>
        /// Retrieves a TLS Certificate based on configuration values.
        /// Allows a TLS certificate to be overridden from a remote file.
        /// </summary>
        /// <param name="config">IConfiguration.</param>
        /// <returns>
        /// Uses the following configuration values:
        /// TlsCertificate:Url
        /// TlsCertificate:Password.
        /// </returns>
        public static X509Certificate2? GetTlsCertificateOverride(this IConfiguration config, ILogger logger)
        {
            var certUrl = config.GetValue<string?>("TlsCertificateOverride:Url", null);
            var certPassword = config.GetValue<string?>("TlsCertificateOverride:Password", null);

            logger.Information("TlsCertificateOverride = {CertUrl} {CertPassLength}", certUrl, certPassword == null ? 0 : certPassword.Length);

            if (string.IsNullOrEmpty(certUrl) || string.IsNullOrEmpty(certPassword))
            {
                logger.Information("TlsCertificateOverride override details not found.");
                return null;
            }

            var cert = new X509Certificate2(DownloadData(certUrl), certPassword, X509KeyStorageFlags.Exportable);
            logger.Information("Downloaded certificate: {Thumbprint}", cert.Thumbprint);
            return cert;
        }

        private static byte[] DownloadData(string url)
        {
            using (var http = new HttpClient())
            {
                byte[] result = Array.Empty<byte>();
                Task.Run(async () => result = await http.GetByteArrayAsync(url)).Wait();
                return result;
            }
        }
    }
}

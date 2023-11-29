using System;
using System.Configuration;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using CDR.DataHolder.Shared.API.Infrastructure.IdPermanence;
using Microsoft.Extensions.Configuration;

namespace CDR.DataHolder.Shared.API.Infrastructure.IdPermanence
{
    /// <summary>
    /// Id Permanence Helper
    /// </summary>
    public static class IdPermanenceHelper
    {

        /// <summary>
        /// Encrypt an ID to meet ID Permanence rules.
        /// </summary>
        /// <param name="internalId">Internal ID (i.e. accountId, transactionId) to encrypt</param>
        /// <param name="idParameters">IdPermanenceParameters</param>
        /// <param name="privateKey">Private Key</param>
        /// <returns>Encrypted ID</returns>
        public static string EncryptId(string? internalId, IdPermanenceParameters? idParameters, string? privateKey)
        {
            if (string.IsNullOrEmpty(internalId))
                throw new ArgumentException("Value is null or empty", nameof(internalId));

            if (string.IsNullOrEmpty(privateKey))
                throw new ArgumentException("Value is null or empty", nameof(privateKey));

            if (idParameters == null)
                throw new ArgumentNullException(nameof(idParameters));

            if (string.IsNullOrEmpty(idParameters.SoftwareProductId))
                throw new ArgumentException($"{nameof(idParameters)}.{nameof(idParameters.SoftwareProductId)} is not supplied.");

            if (string.IsNullOrEmpty(idParameters.CustomerId))
                throw new ArgumentException($"{nameof(idParameters)}.{nameof(idParameters.CustomerId)} is not supplied.");

            var textToEncrypt = $"{idParameters.CustomerId}{internalId}";
            var encryptionKey = $"{idParameters.SoftwareProductId}{privateKey}";
            return Encode(Encrypt(textToEncrypt, encryptionKey));
        }

        /// <summary>
        /// Decrypt an encrypted ID back to the internal value.
        /// </summary>
        /// <param name="encryptedId">Encrypted ID to decrypt back to internal value</param>
        /// <param name="idParameters">IdPermanenceParameters</param>
        /// <param name="privateKey">Private Key</param>
        /// <returns>Internal ID</returns>
        public static string DecryptId(string encryptedId, IdPermanenceParameters idParameters, string privateKey)
        {
            if (string.IsNullOrEmpty(encryptedId))
                throw new ArgumentException("Value is null or empty", nameof(encryptedId));

            if (string.IsNullOrEmpty(privateKey))
                throw new ArgumentException("Value is null or empty", nameof(privateKey));

            if (idParameters == null)
                throw new ArgumentNullException(nameof(idParameters));

            if (string.IsNullOrEmpty(idParameters.SoftwareProductId))
                throw new ArgumentException($"{nameof(idParameters)}.{nameof(idParameters.SoftwareProductId)} is not supplied.");

            if (string.IsNullOrEmpty(idParameters.CustomerId))
                throw new ArgumentException($"{nameof(idParameters)}.{nameof(idParameters.CustomerId)} is not supplied.");

            var encryptionKey = $"{idParameters.SoftwareProductId}{privateKey}";
            var decryptedStr = Decrypt(Decode(encryptedId), encryptionKey);

            // The first substring is the login id
            return decryptedStr.Substring(idParameters.CustomerId.Length);
        }

        /// <summary>
        /// Encrypt the internal customer id for inclusion as the "sub" claim in id_token and access_token.
        /// </summary>
        /// <param name="customerId">Internal Customer Id</param>
        /// <param name="subParameters">SubPermanenceParameters</param>
        /// <param name="privateKey">Private Key</param>
        /// <returns>Encrypted customer id to be included in sub claim</returns>
        public static string EncryptSub(string? customerId, SubPermanenceParameters? subParameters, string? privateKey)
        {
            if (string.IsNullOrEmpty(customerId))
                throw new ArgumentException("Value is null or empty", nameof(customerId));

            if (string.IsNullOrEmpty(privateKey))
                throw new ArgumentException("Value is null or empty", nameof(privateKey));

            if (subParameters == null)
                throw new ArgumentNullException(nameof(subParameters));

            if (string.IsNullOrEmpty(subParameters.SoftwareProductId))
                throw new ArgumentException($"{nameof(subParameters)}.{nameof(subParameters.SoftwareProductId)} is not supplied.");

            var encryptionKey = $"{subParameters.SoftwareProductId}{privateKey}";
            return Encrypt(customerId, encryptionKey);
        }

        /// <summary>
        /// Decrypt the encrypted sub claim value from the access_token into the internal customer id.
        /// </summary>
        /// <param name="sub">Encrypted Customer Id found in sub claim of the access_token</param>
        /// <param name="subParameters">SubPermanenceParameters</param>
        /// <param name="privateKey">Private Key</param>
        /// <returns>Internal Customer Id</returns>
        public static string DecryptSub(string? sub, SubPermanenceParameters? subParameters, string? privateKey)
        {
            if (string.IsNullOrEmpty(sub))
                throw new ArgumentException("Value is null or empty", nameof(sub));

            if (string.IsNullOrEmpty(privateKey))
                throw new ArgumentException("Value is null or empty", nameof(privateKey));

            if (subParameters == null)
                throw new ArgumentNullException(nameof(subParameters));

            if (string.IsNullOrEmpty(subParameters.SoftwareProductId))
                throw new ArgumentException($"{nameof(subParameters)}.{nameof(subParameters.SoftwareProductId)} is not supplied.");

            var encryptionKey = $"{subParameters.SoftwareProductId}{privateKey}";
            return Decrypt(sub, encryptionKey);
        }

        private static string Encrypt(string plaintext, string encryptionKey)
        {
            try
            {
                return Convert.ToBase64String(AesEncryptor.EncryptString(encryptionKey, plaintext));
            }
            catch (Exception ex)
            {
                throw new FormatException("Unable to generate id.", ex.InnerException ?? ex);
            }
        }

        private static string Decrypt(string ciphertext, string encryptionKey)
        {
            try
            {
                return AesEncryptor.DecryptString(encryptionKey, Convert.FromBase64String(ciphertext));
            }
            catch (Exception ex)
            {
                throw new FormatException("Unable to decrypt.", ex.InnerException ?? ex);
            }
        }

        private static string Encode(string value)
        {
            return value.Replace("/", "%2F");
        }

        private static string Decode(string value)
        {
            return value.Replace("%2F", "/");
        }

        public static string GetPrivateKey(IConfiguration config)
        {
            string privateKey = config["IdPermanence:PrivateKey"];

            // Private key was found, so return.
            if (!string.IsNullOrEmpty(privateKey))
            {
                return privateKey;
            }

            // Try loading the private key from a configured certificate.
            var path = config["IdPermanence:Certificate:Path"];
            var pwd = config["IdPermanence:Certificate:Password"];

            if (string.IsNullOrEmpty(path) || string.IsNullOrEmpty(pwd))
            {
                throw new ConfigurationErrorsException($"The private key was not found in configuration.  Either set the \"IdPermanence:PrivateKey\" configuration item or the \"IdPermanence:Certificate:Path\" and \"IdPermanence:Certificate:Password\" configuration items to load the private key.");
            }

            var cert = new X509Certificate2(path, pwd, X509KeyStorageFlags.Exportable);
            return new string(cert.GetRSAPrivateKey()?.ExportPkcs8PrivateKey().Select(b => Convert.ToChar(b)).ToArray());
        }
    }
}

using CDR.DataHolder.IdentityServer.Models;
using CDR.DataHolder.IdentityServer.Services.Interfaces;
using IdentityServer4.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System;
using System.Collections.Generic;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;

namespace CDR.DataHolder.IdentityServer.Services
{
    public class SecurityService : ISecurityService
    {
        private X509SigningCredentials _signingCredentialsPS256;
        private SigningCredentials _signingCredentialsES256;
        private SignatureProvider _signatureProviderPS256;
        private SignatureProvider _signatureProviderES256;
        private List<SecurityKey> _signingKeys;

        public IEnumerable<SecurityKey> SigningKeys
        {
            get
            {
                return _signingKeys;
            }
        }

        public IEnumerable<SigningCredentials> SigningCredentials
        {
            get
            {
                return new SigningCredentials[] { _signingCredentialsPS256, _signingCredentialsES256 };
            }
        }

        public SecurityService(IConfiguration configuration)
        {
            _signingKeys = new List<SecurityKey>();
            CreatePS256SignatureProvider(configuration);
            CreateES256SignatureProvider(configuration);
        }

        private void CreatePS256SignatureProvider(
            IConfiguration configuration)
        {
            // Create the PS256 security key from the ps256 signing certificate.
            var cert = new X509Certificate2(configuration["PS256SigningCertificate:Path"], configuration["PS256SigningCertificate:Password"], X509KeyStorageFlags.Exportable);
            var securityKey = new X509SecurityKey(cert);
            _signingKeys.Add(securityKey);

            // Create PS256 x509 credentials
            _signingCredentialsPS256 = new X509SigningCredentials(cert, SecurityAlgorithms.RsaSsaPssSha256)
            {
                CryptoProviderFactory = new CryptoProviderFactory()
            };

            //Create PS256 signature provider
            _signatureProviderPS256 = _signingCredentialsPS256.CryptoProviderFactory.CreateForSigning(securityKey, SecurityAlgorithms.RsaSsaPssSha256);
        }

        private void CreateES256SignatureProvider(
            IConfiguration configuration)
        {
            // Create the ES256 security key from the es256 signing certificate.
            var cert = new X509Certificate2(configuration["ES256SigningCertificate:Path"], configuration["ES256SigningCertificate:Password"], X509KeyStorageFlags.Exportable);
            var ecdsa = cert.GetECDsaPrivateKey();
            var securityKey = new ECDsaSecurityKey(ecdsa) { KeyId = cert.Thumbprint };
            _signingKeys.Add(securityKey);

            // Create ES256 signing credentials
            _signingCredentialsES256 = new SigningCredentials(securityKey, SecurityAlgorithms.EcdsaSha256)
            {
                CryptoProviderFactory = new CryptoProviderFactory()
            };

            // Create ES256 signature provider
            _signatureProviderES256 = _signingCredentialsES256.CryptoProviderFactory.CreateForSigning(securityKey, SecurityAlgorithms.EcdsaSha256);
        }

        public Task<byte[]> Sign(string algorithm, byte[] digest)
        {
            //Select signature provider based on algorithm
            var signatureProvider =
            algorithm switch
            {
                SecurityAlgorithms.RsaSsaPssSha256 => _signatureProviderPS256,
                SecurityAlgorithms.EcdsaSha256 => _signatureProviderES256,
                _ => throw new ApplicationException("Invalid algorithm")
            };

            //Sign the digest
            var signature = signatureProvider.Sign(digest);

            return Task.FromResult(signature);
        }

        public async Task<SecurityKeyInfo[]> GetActiveSecurityKeys(string algorithm)
        {
            var activeKeys = new List<SecurityKeyInfo>();

            //Select security key based on algorithm
            SecurityKeyInfo securityKeyInfo =
            algorithm switch
            {
                SecurityAlgorithms.RsaSsaPssSha256 => new RsaSecurityKeyInfo(_signingCredentialsPS256),
                SecurityAlgorithms.EcdsaSha256 => new ECDsaSecurityKeyInfo(_signingCredentialsES256),
                _ => throw new ApplicationException("Invalid algorithm")
            };

            activeKeys.Add(securityKeyInfo);

            return await Task.FromResult(activeKeys.ToArray());
        }

    }
}
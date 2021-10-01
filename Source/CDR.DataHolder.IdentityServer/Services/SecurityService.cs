using System;
using System.Collections.Generic;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using CDR.DataHolder.IdentityServer.Models;
using CDR.DataHolder.IdentityServer.Services.Interfaces;
using IdentityServer4.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace CDR.DataHolder.IdentityServer.Services
{
    public class SecurityService : ISecurityService
    {
        private readonly X509SigningCredentials _signingCredentialsPS256;
        private readonly X509SigningCredentials _signingCredentialsES256;
        private readonly SignatureProvider _signatureProviderPS256;
        private readonly SignatureProvider _signatureProviderES256;

        public SecurityService(IConfiguration configuration)
        {
            //Read certificate path & pwd from config
            var filePath = configuration["SigningCertificate:Path"];
            var pwd = configuration["SigningCertificate:Password"];

            //Open certificate
            var cert = new X509Certificate2(filePath, pwd, X509KeyStorageFlags.Exportable);
            var securityKey = new X509SecurityKey(cert);

            //Create PS256 x509 credentials
            var credentials = new X509SigningCredentials(cert, SecurityAlgorithms.RsaSsaPssSha256);
            credentials.CryptoProviderFactory = new CryptoProviderFactory();
            _signingCredentialsPS256 = credentials;

            //Create ES256 x509 credentials
            credentials = new X509SigningCredentials(cert, SecurityAlgorithms.EcdsaSha256);
            credentials.CryptoProviderFactory = new CryptoProviderFactory();
            _signingCredentialsES256 = credentials;

            //Create PS256 signature provider
            _signatureProviderPS256 = _signingCredentialsPS256.CryptoProviderFactory.CreateForSigning(securityKey, SecurityAlgorithms.RsaSsaPssSha256);

            //Create ES256 signature provider
            //_signatureProviderES256 = _signingCredentialsES256.CryptoProviderFactory.CreateForSigning(securityKey, SecurityAlgorithms.EcdsaSha256);
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
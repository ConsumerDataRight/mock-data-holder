using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using Microsoft.IdentityModel.Tokens;
using Newtonsoft.Json;

#nullable enable

namespace CDR.DataHolder.IntegrationTests.Infrastructure.API2
{
    public class JWKSBuilder
    {
        public class JWKS
        {
#pragma warning disable IDE1006
            public JWK[]? keys { get; set; }
#pragma warning restore IDE1006
        }

        public class JWK
        {
#pragma warning disable IDE1006
            public string? alg { get; set; }
            public string? e { get; set; }
            // public string[]? key_ops { get; set; }
            public string? kid { get; set; }
            public string? kty { get; set; }
            public string? n { get; set; }
            public string? use { get; set; }
#pragma warning restore IDE1006
        }        

        /// <summary>
        /// Build JWKS from certificate
        /// </summary>
        public static JWKS Build(string certificateFilename, string certificatePassword)
        {
            var cert = new X509Certificate2(certificateFilename, certificatePassword);

            //Get credentials from certificate
            var securityKey = new X509SecurityKey(cert);
            var signingCredentials = new X509SigningCredentials(cert, SecurityAlgorithms.RsaSsaPssSha256);
            var encryptingCredentials = new X509EncryptingCredentials(cert, SecurityAlgorithms.RsaOaepKeyWrap, SecurityAlgorithms.RsaOAEP);

            var rsaParams = signingCredentials?.Certificate?.GetRSAPublicKey()?.ExportParameters(false) ?? throw new Exception("Error getting RSA params");
            var e = Base64UrlEncoder.Encode(rsaParams.Exponent);
            var n = Base64UrlEncoder.Encode(rsaParams.Modulus);

            var jwkSign = new JWK()
            {
                alg = signingCredentials.Algorithm,
                kid = signingCredentials.Kid, 
                //  kid = signingCredentials.Key.KeyId,
                kty = securityKey.PublicKey.KeyExchangeAlgorithm,
                n = n,
                e = e,
                use = "sig"
            };

            var jwkEnc = new JWK()
            {
                alg = encryptingCredentials.Enc,
                kid = encryptingCredentials.Key.KeyId,
                kty = securityKey.PublicKey.KeyExchangeAlgorithm,
                n = n,
                e = e,
                use = "enc"
            };

            return new JWKS()
            {
                keys = new JWK[] { jwkSign, jwkEnc }
            };
        }
    }
}
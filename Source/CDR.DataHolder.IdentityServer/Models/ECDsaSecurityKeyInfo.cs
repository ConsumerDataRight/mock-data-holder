using System.Security.Cryptography;
using IdentityServer4.Models;
using Microsoft.IdentityModel.Tokens;
using Org.BouncyCastle.Asn1.Sec;

namespace CDR.DataHolder.IdentityServer.Models
{
    public class ECDsaSecurityKeyInfo : SecurityKeyInfo
    {
        public ECDsaSecurityKeyInfo(X509SigningCredentials signingCredentials)
        {
            Key = new ECDsaSecurityKey(LoadPrivateKey(signingCredentials.Certificate.PrivateKey.ExportPkcs8PrivateKey())) { KeyId = signingCredentials.Kid };
            SigningAlgorithm = SecurityAlgorithms.EcdsaSha256;
        }

        private static ECDsa LoadPrivateKey(byte[] key)
        {
            var privKeyInt = new Org.BouncyCastle.Math.BigInteger(+1, key);
            var parameters = SecNamedCurves.GetByName("secp256r1");
            var ecPoint = parameters.G.Multiply(privKeyInt);
            var privKeyX = ecPoint.Normalize().XCoord.ToBigInteger().ToByteArrayUnsigned();
            var privKeyY = ecPoint.Normalize().YCoord.ToBigInteger().ToByteArrayUnsigned();

            return ECDsa.Create(new ECParameters
            {
                Curve = ECCurve.NamedCurves.nistP256,
                Q = new ECPoint
                {
                    X = privKeyX,
                    Y = privKeyY
                }
            });
        }
    }
}
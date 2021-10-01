using System.Security.Cryptography.X509Certificates;
using IdentityServer4.Models;
using Microsoft.IdentityModel.Tokens;

namespace CDR.DataHolder.IdentityServer.Models
{
    public class RsaSecurityKeyInfo : SecurityKeyInfo
    {
        public RsaSecurityKeyInfo(X509SigningCredentials signingCredentials)
        {
            Key = new RsaSecurityKey(signingCredentials.Certificate.GetRSAPrivateKey()) { KeyId = signingCredentials.Kid };
            SigningAlgorithm = SecurityAlgorithms.RsaSsaPssSha256;
        }
    }
}
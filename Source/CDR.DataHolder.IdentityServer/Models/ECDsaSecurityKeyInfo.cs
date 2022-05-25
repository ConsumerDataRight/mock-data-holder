using IdentityServer4.Models;
using Microsoft.IdentityModel.Tokens;

namespace CDR.DataHolder.IdentityServer.Models
{
    public class ECDsaSecurityKeyInfo : SecurityKeyInfo
    {
        public ECDsaSecurityKeyInfo(SigningCredentials signingCredentials)
        {
            Key = signingCredentials.Key;
            SigningAlgorithm = SecurityAlgorithms.EcdsaSha256;
        }
    }
}
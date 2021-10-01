using IdentityServer4.Models;
using Microsoft.IdentityModel.Tokens;

namespace CDR.DataHolder.IdentityServer.Models
{
    public class RotationalSigningCredentials : SigningCredentials
    {
        public RotationalSigningCredentials(SecurityKeyInfo keyInfo, bool newlyCreated = false)
            : base(keyInfo.Key, keyInfo.SigningAlgorithm)
        {
            NewlyCreated = newlyCreated;
        }

        public bool NewlyCreated { get; }
    }
}
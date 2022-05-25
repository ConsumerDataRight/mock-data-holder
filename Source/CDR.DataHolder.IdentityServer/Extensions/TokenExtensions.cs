using CDR.DataHolder.API.Infrastructure.Extensions;
using IdentityServer4.Models;
namespace CDR.DataHolder.IdentityServer.Extensions
{
    public static class TokenExtensions
    {
        public static int GetExpiry(this RefreshToken refreshToken)
        {
            return refreshToken.CreationTime.AddSeconds(refreshToken.Lifetime).ToEpoch();
        }
    }
}
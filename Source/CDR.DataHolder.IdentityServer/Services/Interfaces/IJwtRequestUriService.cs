using System.IdentityModel.Tokens.Jwt;
using System.Threading.Tasks;
using IdentityServer4.Models;

namespace CDR.DataHolder.IdentityServer.Services.Interfaces
{
    public interface IJwtRequestUriService
    {
        Task<JwtSecurityToken> GetJwtAsync(string jwtRequestUri, Client client);
    }
}

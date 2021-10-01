using System.Threading.Tasks;
using IdentityServer4.Models;

namespace CDR.DataHolder.IdentityServer.Interfaces
{
    public interface IJwtTokenCreationService
    {
        public Task<string> CreateTokenAsync(Token token);
    }
}

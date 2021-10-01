using System;
using System.Threading.Tasks;
using Microsoft.IdentityModel.Tokens;

namespace CDR.DataHolder.IdentityServer.Interfaces
{
    public interface IJwksService
    {
        Task<JsonWebKeySet> GetJwks(Uri jwksUri);
    }
}
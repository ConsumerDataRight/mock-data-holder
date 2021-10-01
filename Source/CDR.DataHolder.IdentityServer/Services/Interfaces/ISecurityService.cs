using System.Threading.Tasks;
using IdentityServer4.Models;

namespace CDR.DataHolder.IdentityServer.Services.Interfaces
{
    public interface ISecurityService
    {
        Task<SecurityKeyInfo[]> GetActiveSecurityKeys(string algorithm);
        Task<byte[]> Sign(string algorithm, byte[] digest);
    }
}
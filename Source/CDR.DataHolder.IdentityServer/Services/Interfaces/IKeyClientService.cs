using System;
using System.Threading.Tasks;
using CDR.DataHolder.IdentityServer.Models;

namespace CDR.DataHolder.IdentityServer.Services.Interfaces
{
    public interface IKeyClientService
    {
        Task<IKeyVaultKeyResponse> CreateKeyAsync(string keyName, KeyVaultKeyType keyType, DateTimeOffset absoluteExpiration);

        Task<byte[]> SignAsync(string keyName, string algorithm, byte[] digest);

        Task<IKeyVaultKeyResponse> GetKeyAsync(string keyName, string keyVersion = null);

        Task<IKeyVaultKeyProperties[]> GetKeyPropertiesAsync(string keyName);
    }
}
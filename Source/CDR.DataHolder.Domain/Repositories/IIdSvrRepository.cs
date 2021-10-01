using System;
using System.Threading.Tasks;
using CDR.DataHolder.Domain.Entities;

namespace CDR.DataHolder.Domain.Repositories
{
    public interface IIdSvrRepository
    {
        Task<UserInfoClaims> GetUserInfoClaims(Guid customerId);
        Task<SoftwareProduct> GetSoftwareProduct(Guid softwareProductId);
    }
}

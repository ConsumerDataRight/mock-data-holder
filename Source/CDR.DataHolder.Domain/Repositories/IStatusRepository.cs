using System;
using System.Threading.Tasks;
using CDR.DataHolder.Domain.Entities;

namespace CDR.DataHolder.Domain.Repositories
{
    public interface IStatusRepository
    {
        Task<SoftwareProduct> GetSoftwareProduct(Guid softwareProductId);
        Task RefreshDataRecipients(string dataRecipientJson); 
        Task UpdateDataRecipientStatus(DataRecipientStatus dataRecipientStatus);
        Task UpdateSoftwareProductStatus(SoftwareProductStatus softwareProductStatus);
    }
}
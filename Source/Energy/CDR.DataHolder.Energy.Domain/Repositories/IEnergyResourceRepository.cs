using CDR.DataHolder.Energy.Domain.Entities;
using CDR.DataHolder.Energy.Domain.ValueObjects;
using CDR.DataHolder.Shared.Domain.ValueObjects;
using CDR.DataHolder.Shared.Repository;
using System;
using System.Threading.Tasks;

namespace CDR.DataHolder.Energy.Domain.Repositories
{
    public interface IEnergyResourceRepository : ICommonRepository
    {
        Task<Page<EnergyAccount[]>> GetAllEnergyAccounts(AccountFilter filter, int page, int pageSize);

        Task<EnergyAccount[]> GetAllAccountsByCustomerIdForConsent(Guid customerId);

        Task<EnergyAccountConcession[]> GetEnergyAccountConcessions(AccountConcessionsFilter filter);
    }
}

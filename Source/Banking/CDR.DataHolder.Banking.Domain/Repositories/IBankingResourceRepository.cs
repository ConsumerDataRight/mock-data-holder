using CDR.DataHolder.Banking.Domain.ValueObjects;
using CDR.DataHolder.Shared.Domain.ValueObjects;
using CDR.DataHolder.Shared.Repository;
using System;
using System.Threading.Tasks;
using Bank = CDR.DataHolder.Banking.Domain.Entities;

namespace CDR.DataHolder.Banking.Domain.Repositories
{
    public interface IBankingResourceRepository : ICommonRepository
    {                
        Task<Page<Entities.Account[]>> GetAllAccounts(AccountFilter filter, int page, int pageSize);
        Task<Entities.Account[]> GetAllAccountsByCustomerIdForConsent(Guid customerId);
        Task<Page<Bank.AccountTransaction[]>> GetAccountTransactions(AccountTransactionsFilter transactionsFilter, int page, int pageSize);
    }
}

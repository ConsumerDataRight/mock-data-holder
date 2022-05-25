using System;
using System.Threading.Tasks;
using CDR.DataHolder.Domain.Entities;
using CDR.DataHolder.Domain.ValueObjects;

namespace CDR.DataHolder.Domain.Repositories
{
	public interface IResourceRepository
	{
		Task<Customer> GetCustomer(Guid customerId);
		Task<Customer> GetCustomerByLoginId(string loginId);
		Task<bool> CanAccessAccount(string accountId, Guid customerId);
		Task<Page<Account[]>> GetAllAccounts(AccountFilter filter, int page, int pageSize);
		Task<Account[]> GetAllAccountsByCustomerIdForConsent(Guid customerId);
		Task<Page<AccountTransaction[]>> GetAccountTransactions(AccountTransactionsFilter transactionsFilter, int page, int pageSize);
	}
}

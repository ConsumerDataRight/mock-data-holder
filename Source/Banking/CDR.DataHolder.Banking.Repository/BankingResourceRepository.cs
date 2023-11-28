using AutoMapper;
using CDR.DataHolder.Banking.Domain.Entities;
using CDR.DataHolder.Banking.Domain.Repositories;
using CDR.DataHolder.Banking.Domain.ValueObjects;
using CDR.DataHolder.Banking.Repository.Infrastructure;
using CDR.DataHolder.Shared.Domain.ValueObjects;
using CDR.DataHolder.Shared.Repository;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace CDR.DataHolder.Banking.Repository
{
    public class BankingResourceRepository : IBankingResourceRepository
	{
		private readonly BankingDataHolderDatabaseContext _dataHolderDatabaseContext;
		private readonly IMapper _mapper;

		public BankingResourceRepository(BankingDataHolderDatabaseContext dataHolderDatabaseContext, IMapper mapper)
		{
			_dataHolderDatabaseContext = dataHolderDatabaseContext;
			_mapper = mapper;
		}

		public async Task<Shared.Domain.Entities.Customer?> GetCustomer(Guid customerId)
		{
			var customer = await _dataHolderDatabaseContext.Customers.AsNoTracking()
				.Include(p => p.Person)
				.Include(o => o.Organisation)
				.FirstOrDefaultAsync(customer => customer.CustomerId == customerId);
			if (customer == null)
			{
				return null;
			}

			switch (customer.CustomerUType?.ToLower())
			{
				case "organisation":
					return _mapper.Map<Organisation>(customer);					
				case "person":
					return _mapper.Map<Person>(customer);

				default:
					return null;
			}
		}

		public async Task<Shared.Domain.Entities.Customer?> GetCustomerByLoginId(string loginId)
		{
			var customer = await _dataHolderDatabaseContext.Customers.AsNoTracking()
                .Include(p => p.Person)
                .Include(o => o.Organisation)
                .FirstOrDefaultAsync(customer => customer.LoginId == loginId);

            if (customer == null)
            {
                return null;
            }

            switch (customer.CustomerUType?.ToLower())
            {
                case "organisation":
                    return _mapper.Map<Organisation>(customer);

                case "person":
                    return _mapper.Map<Person>(customer);

                default:
                    return null;
            }
		}
		
        public async Task<Page<Account[]>> GetAllAccounts(AccountFilter filter, int page, int pageSize)
		{
			var result = new Page<Account[]>()
			{
				Data = Array.Empty<Account>(),
				CurrentPage = page,
				PageSize = pageSize,
			};

			// We always return accounts for the individual. We don't have a concept of joint or shared accounts at the moment
			// So, if asked from accounts which rent owned, just return empty result.
			if (filter.IsOwned.HasValue && !filter.IsOwned.Value)
			{
				return result;
			}

			// If none of the account ids are allowed, return empty list
			if (filter.AllowedAccountIds == null || !filter.AllowedAccountIds.Any())
			{
				return result;
			}

			IQueryable<Banking.Repository.Entities.Account> accountsQuery = _dataHolderDatabaseContext.Accounts.AsNoTracking()
				.Include(account => account.Customer)
				.Where(account =>
					filter.AllowedAccountIds.Contains(account.AccountId));

			// Apply filters.
			if (!string.IsNullOrEmpty(filter.OpenStatus))
			{
				accountsQuery = accountsQuery.Where(account => account.OpenStatus == filter.OpenStatus);
			}
			if (!string.IsNullOrEmpty(filter.ProductCategory))
			{
				accountsQuery = accountsQuery.Where(account => account.ProductCategory == filter.ProductCategory);
			}

			var totalRecords = await accountsQuery.CountAsync();

			// Apply ordering and pagination
			accountsQuery = accountsQuery
				.OrderBy(account => account.DisplayName).ThenBy(account => account.AccountId)
				.Skip((page - 1) * pageSize)
				.Take(pageSize);

			var accounts = await accountsQuery.ToListAsync();
			result.Data = _mapper.Map<Account[]>(accounts);
			result.TotalRecords = totalRecords;

			return result;
		}

		/// <summary>
		/// Check that the customer can access the given accounts.
		/// </summary>
		/// <param name="accountId">Account ID is primary key</param>		
		/// <returns>True if the customer can access the account, otherwise false.</returns>
		public async Task<bool> CanAccessAccount(string accountId)
		{
			return await _dataHolderDatabaseContext.Accounts.AnyAsync(a => a.AccountId == accountId);
		}

		/// <summary>
		/// Get a list of all transactions for a given account.
		/// </summary>
		/// <param name="transactionsFilter">Query filter</param>
		/// <param name="page">Page number</param>
		/// <param name="pageSize">Page size</param>
		/// <returns></returns>
		public async Task<Page<AccountTransaction[]>> GetAccountTransactions(AccountTransactionsFilter transactionsFilter, int page, int pageSize)
		{
			if (!transactionsFilter.NewestTime.HasValue)
			{
				transactionsFilter.NewestTime = DateTime.UtcNow;
			}

			if (!transactionsFilter.OldestTime.HasValue)
			{
				transactionsFilter.OldestTime = transactionsFilter.NewestTime.Value.AddDays(-90);
			}

			var result = new Page<AccountTransaction[]>()
			{
				Data = Array.Empty<AccountTransaction>(),
				CurrentPage = page,
				PageSize = pageSize,
			};

            IQueryable<Banking.Repository.Entities.Transaction> accountTransactionsQuery = _dataHolderDatabaseContext
                            .Transactions.Include(x => x.Account).ThenInclude(x => x.Customer).AsNoTracking()                    
                    .Where(t => t.AccountId == transactionsFilter.AccountId)
                    // Oldest/Newest Time
                    //Newest
                    .WhereIf(transactionsFilter.NewestTime.HasValue,
							 t => (t.PostingDateTime ?? t.ExecutionDateTime) <= transactionsFilter.NewestTime)
					//Oldest
					.WhereIf(transactionsFilter.OldestTime.HasValue,
							 t => (t.PostingDateTime ?? t.ExecutionDateTime) >= transactionsFilter.OldestTime)

                    // Min/Max Amount
                    //Min
                    .WhereIf(transactionsFilter.MinAmount.HasValue,
							 t => t.Amount >= transactionsFilter.MinAmount)
					//Max
					.WhereIf(transactionsFilter.MaxAmount.HasValue,
							 t => t.Amount <= transactionsFilter.MaxAmount)				

					//Text
                    .WhereIf(!string.IsNullOrEmpty(transactionsFilter.Text), 
							 t => EF.Functions.Like(t.Description, $"%{transactionsFilter.Text}%") || EF.Functions.Like(t.Reference, $"%{transactionsFilter.Text}%"));

            var totalRecords = await accountTransactionsQuery.CountAsync();

            // Apply ordering and pagination
            accountTransactionsQuery = accountTransactionsQuery
                .OrderByDescending(t => t.PostingDateTime).ThenByDescending(t => t.ExecutionDateTime)
                .Skip((page - 1) * pageSize)
                .Take(pageSize);

            var transactions = await accountTransactionsQuery.ToListAsync();
            result.Data = _mapper.Map<AccountTransaction[]>(transactions);
            result.TotalRecords = totalRecords;

            return result;
        }

        public async Task<Account[]> GetAllAccountsByCustomerIdForConsent(Guid customerId)
        {
            var allAccounts = await _dataHolderDatabaseContext.Accounts.AsNoTracking()
                .Include(account => account.Customer)
                .Where(account => account.Customer.CustomerId == customerId)
                .OrderBy(account => account.DisplayName).ThenBy(account => account.AccountId)
                .ToListAsync();

            return _mapper.Map<Account[]>(allAccounts);
        }		        
    }
}

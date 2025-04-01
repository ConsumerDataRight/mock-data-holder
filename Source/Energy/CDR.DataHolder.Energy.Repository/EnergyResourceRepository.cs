using AutoMapper;
using CDR.DataHolder.Energy.Domain.Entities;
using CDR.DataHolder.Energy.Domain.Repositories;
using CDR.DataHolder.Energy.Domain.ValueObjects;
using CDR.DataHolder.Energy.Repository.Infrastructure;
using CDR.DataHolder.Shared.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace CDR.DataHolder.Energy.Repository
{
    public class EnergyResourceRepository : IEnergyResourceRepository
    {
        private readonly EnergyDataHolderDatabaseContext _dataHolderDatabaseContext;
        private readonly IMapper _mapper;

        public EnergyResourceRepository(EnergyDataHolderDatabaseContext dataHolderDatabaseContext, IMapper mapper)
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

            return customer.CustomerUType?.ToLower() switch
            {
                "organisation" => _mapper.Map<Organisation>(customer),
                "person" => _mapper.Map<Person>(customer),
                _ => null,
            };
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

            return customer.CustomerUType?.ToLower() switch
            {
                "organisation" => _mapper.Map<Organisation>(customer),
                "person" => _mapper.Map<Person>(customer),
                _ => null,
            };
        }

        public async Task<Page<EnergyAccount[]>> GetAllEnergyAccounts(AccountFilter filter, int page, int pageSize)
        {
            var result = new Page<EnergyAccount[]>()
            {
                Data = Array.Empty<EnergyAccount>(),
                CurrentPage = page,
                PageSize = pageSize,
            };

            // If none of the account ids are allowed, return empty list
            if (filter.AllowedAccountIds == null || filter.AllowedAccountIds.Length == 0)
            {
                return result;
            }

            IQueryable<Entities.Account> accountsQuery = _dataHolderDatabaseContext.Accounts.AsNoTracking()
                .Include(account => account.Customer)
                .Include(account => account.AccountPlans!)
                    .ThenInclude(accountPlan => accountPlan.PlanOverview)
                .Include(account => account.AccountPlans!)
                    .ThenInclude(accountPlan => accountPlan.ServicePoints)
                .Where(account =>
                    filter.AllowedAccountIds.Contains(account.AccountId));

            // Apply open status filter.
            if (!string.IsNullOrEmpty(filter.OpenStatus))
            {
                accountsQuery = accountsQuery.Where(account => account.OpenStatus == filter.OpenStatus);
            }

            // Apply ordering and pagination
            var totalRecords = await accountsQuery.CountAsync();
            accountsQuery = accountsQuery
                .OrderBy(account => account.DisplayName).ThenBy(account => account.AccountId)
                .Skip((page - 1) * pageSize)
                .Take(pageSize);

            var accounts = await accountsQuery.ToListAsync();
            result.Data = _mapper.Map<EnergyAccount[]>(accounts);
            result.TotalRecords = totalRecords;

            return result;
        }

        /// <summary>
        /// Check that the customer can access the given accounts.
        /// </summary>
        /// <param name="accountId">Account ID.</param>
        /// <returns>True if the customer can access the account, otherwise false.</returns>
        public async Task<bool> CanAccessAccount(string accountId)
        {
            return await _dataHolderDatabaseContext.Accounts.AnyAsync(a => a.AccountId == accountId);
        }

        /// <summary>
        /// Get a list of all concession for a given account.
        /// </summary>
        /// <param name="filter">Query filter.</param>
        /// <returns></returns>
        public async Task<EnergyAccountConcession[]> GetEnergyAccountConcessions(AccountConcessionsFilter filter)
        {
            IQueryable<Entities.AccountConcession> accountTransactionsQuery = _dataHolderDatabaseContext.AccountConcessions.AsNoTracking()
                .Where(accountConcession => accountConcession.AccountId == filter.AccountId);
            var concessions = await accountTransactionsQuery.ToListAsync();

            return _mapper.Map<EnergyAccountConcession[]>(concessions);
        }

        public async Task<EnergyAccount[]> GetAllAccountsByCustomerIdForConsent(Guid customerId)
        {
            var allAccounts = await _dataHolderDatabaseContext.Accounts.AsNoTracking()
                .Include(account => account.Customer)
                .Where(account => account.Customer.CustomerId == customerId)
                .OrderBy(account => account.DisplayName).ThenBy(account => account.AccountId)
                .ToListAsync();

            return _mapper.Map<EnergyAccount[]>(allAccounts);
        }
    }
}

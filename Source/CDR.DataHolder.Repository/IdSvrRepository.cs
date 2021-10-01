using System;
using System.Linq;
using System.Threading.Tasks;
using CDR.DataHolder.Domain.Entities;
using CDR.DataHolder.Domain.Repositories;
using CDR.DataHolder.Repository.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace CDR.DataHolder.Repository
{
    public class IdSvrRepository : IIdSvrRepository
	{
		private readonly DataHolderDatabaseContext _dataHolderDatabaseContext;
		private const string CUSTOMERUTYPE_PERSON = "PERSON";

		public IdSvrRepository(DataHolderDatabaseContext dataHolderDatabaseContext)
		{
			_dataHolderDatabaseContext = dataHolderDatabaseContext;
		}

		public async Task<UserInfoClaims> GetUserInfoClaims(Guid customerId)
		{
			return await _dataHolderDatabaseContext.Customers
				.Include(x => x.Person)
				.Include(x => x.Organisation)
				.Where(x => x.CustomerId == customerId)
				.Select(x => new UserInfoClaims
				{
					GivenName = x.Person != null && x.CustomerUType == CUSTOMERUTYPE_PERSON ? x.Person.FirstName : x.Organisation.AgentFirstName,
					FamilyName = x.Person != null && x.CustomerUType == CUSTOMERUTYPE_PERSON ? x.Person.LastName : x.Organisation.AgentLastName,
					Name = x.Person != null && x.CustomerUType == CUSTOMERUTYPE_PERSON ? $"{(!string.IsNullOrEmpty(x.Person.FirstName) ? (x.Person.FirstName + " ") : string.Empty)}{x.Person.LastName}" : x.Organisation.BusinessName,
					LastUpdated = x.Person != null && x.CustomerUType == CUSTOMERUTYPE_PERSON ? x.Person.LastUpdateTime : x.Organisation.LastUpdateTime,
				})
				.FirstOrDefaultAsync();
		}

		public async Task<SoftwareProduct> GetSoftwareProduct(Guid softwareProductId)
		{
			return await _dataHolderDatabaseContext.SoftwareProducts.AsNoTracking()
				.Include(softwareProduct => softwareProduct.Brand.LegalEntity)
				.Where(softwareProduct => softwareProduct.SoftwareProductId == softwareProductId)
				.Select(x => new SoftwareProduct()
				{
					SoftwareProductId = x.SoftwareProductId,
					Status = x.Status
				})
				.FirstOrDefaultAsync();
		}
	}
}

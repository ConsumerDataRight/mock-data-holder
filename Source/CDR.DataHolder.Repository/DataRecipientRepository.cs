using CDR.DataHolder.Repository.Entities;
using CDR.DataHolder.Repository.Infrastructure;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CDR.DataHolder.Repository
{
    public class DataRecipientRepository
    {
        private readonly DbContextOptions<DataHolderDatabaseContext> _options;

        public DataRecipientRepository(string connString)
        {
            _options = new DbContextOptionsBuilder<DataHolderDatabaseContext>().UseSqlServer(connString).Options;
        }
        
        public async Task<Exception> InsertDataRecipient(LegalEntity regDataRecipient)
        {
            try
            {
                using (var dhDbContext = new DataHolderDatabaseContext(_options))
                {
                    using (var txn = dhDbContext.Database.BeginTransaction())
                    {
                        // Insert LegalEntity entity including its child Brands and SoftwareProducts entities
                        dhDbContext.Add(regDataRecipient);
                        await dhDbContext.SaveChangesAsync();
                        await txn.CommitAsync();
                    }
                }
                return null;
            }
            catch (Exception ex)
            {
                return ex;
            }
        }

        public async Task<Exception> DeleteDataRecipients(IList<LegalEntity> dhDataRecipients)
        {
            try
            {
                using (var dhDbContext = new DataHolderDatabaseContext(_options))
                {
                    using (var txn = dhDbContext.Database.BeginTransaction())
                    {
                        dhDbContext.RemoveRange(dhDataRecipients);
                        await dhDbContext.SaveChangesAsync();
                        await txn.CommitAsync();
                    }
                }
                return null;
            }
            catch (Exception ex)
            {
                return ex;
            }
        }

        public async Task<Exception> InsertBrand(Brand regDrBrand)
        {
            try
            {
                using (var dhDbContext = new DataHolderDatabaseContext(_options))
                {
                    using (var txn = dhDbContext.Database.BeginTransaction())
                    {
                        // Insert the Register Brand entities
                        Brand brand = new()
                        {
                            BrandId = regDrBrand.BrandId,
                            BrandName = regDrBrand.BrandName,
                            LogoUri = regDrBrand.LogoUri,
                            Status = regDrBrand.Status,
                            LegalEntityId = regDrBrand.LegalEntityId
                        };

                        dhDbContext.Add(brand);
                        await dhDbContext.SaveChangesAsync();
                        await txn.CommitAsync();
                    }
                }
                return null;
            }
            catch (Exception ex)
            {
                return ex;
            }
        }

        public async Task<Exception> DeleteBrands(IList<Brand> dhBrands)
        {
            try
            {
                using (var dhDbContext = new DataHolderDatabaseContext(_options))
                {
                    using (var txn = dhDbContext.Database.BeginTransaction())
                    {
                        dhDbContext.RemoveRange(dhBrands);
                        await dhDbContext.SaveChangesAsync();
                        await txn.CommitAsync();
                    }
                }
                return null;
            }
            catch (Exception ex)
            {
                return ex;
            }
        }
    }
}
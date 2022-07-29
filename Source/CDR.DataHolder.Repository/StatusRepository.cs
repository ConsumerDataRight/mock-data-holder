using AutoMapper;
using CDR.DataHolder.Domain.Entities;
using CDR.DataHolder.Domain.Repositories;
using CDR.DataHolder.Repository.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace CDR.DataHolder.Repository
{
    public class StatusRepository : IStatusRepository
    {
        private readonly DataHolderDatabaseContext _dataHolderDatabaseContext;
        private readonly IMapper _mapper;

        public StatusRepository(DataHolderDatabaseContext dataHolderDatabaseContext, IMapper mapper)
        {
            this._dataHolderDatabaseContext = dataHolderDatabaseContext;
            this._mapper = mapper;
        }

        public async Task<SoftwareProduct> GetSoftwareProduct(Guid softwareProductId)
        {
            var softwareProduct = await _dataHolderDatabaseContext.SoftwareProducts.AsNoTracking()
                .Include(softwareProduct => softwareProduct.Brand.LegalEntity)
                .Where(softwareProduct => softwareProduct.SoftwareProductId == softwareProductId)
                .FirstOrDefaultAsync();

            return _mapper.Map<SoftwareProduct>(softwareProduct);
        }

        public async Task RefreshDataRecipients(string dataRecipientJson)
        {
            using (var transaction = _dataHolderDatabaseContext.Database.BeginTransaction())
            {
                // Remove the existing data recipients.
                var legalEntities = await _dataHolderDatabaseContext.LegalEntities.AsNoTracking().ToListAsync();
                _dataHolderDatabaseContext.RemoveRange(legalEntities);
                _dataHolderDatabaseContext.SaveChanges();

                // Bulk insert the new data recipient list.
                var data = JsonConvert.DeserializeObject<JObject>(dataRecipientJson);
                var dataRecipients = data["data"].ToObject<Repository.Entities.LegalEntity[]>();
                _dataHolderDatabaseContext.LegalEntities.AddRange(dataRecipients);
                _dataHolderDatabaseContext.SaveChanges();

                // Commit the transaction.
                transaction.Commit();
            }
        }

        public async Task UpdateDataRecipientStatus(DataRecipientStatus dataRecipientStatus)
        {
            var legalEntity = await _dataHolderDatabaseContext.LegalEntities.FindAsync(new Guid(dataRecipientStatus.ID));

            if (legalEntity == null)
            {
                return;
            }

            legalEntity.Status = dataRecipientStatus.Status;
            _dataHolderDatabaseContext.LegalEntities.Update(legalEntity);
            await _dataHolderDatabaseContext.SaveChangesAsync();
        }

        public async Task UpdateSoftwareProductStatus(SoftwareProductStatus softwareProductStatus)
        {
            var softwareProduct = await _dataHolderDatabaseContext.SoftwareProducts.FindAsync(new Guid(softwareProductStatus.ID));

            if (softwareProduct == null)
            {
                return;
            }

            softwareProduct.Status = softwareProductStatus.Status;
            _dataHolderDatabaseContext.SoftwareProducts.Update(softwareProduct);
            await _dataHolderDatabaseContext.SaveChangesAsync();
        }

    }
}

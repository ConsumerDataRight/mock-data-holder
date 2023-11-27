using CDR.DataHolder.Banking.Repository;
using CDR.DataHolder.Energy.Repository;
using CDR.DataHolder.Shared.API.Infrastructure.Exceptions;
using CDR.DataHolder.Shared.Domain;
using CDR.DataHolder.Shared.Repository;

namespace CDR.DataHolder.Common.API.Infrastructure
{
    public class CommonRepositoryFactory : ICommonRepositoryFactory
    {
        private readonly IServiceProvider _serviceProvider;

        public CommonRepositoryFactory(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        public ICommonRepository GetCommonRepository(string industry)
        {
            ICommonRepository? repository = industry switch
            {
                Constants.Industry.Banking => _serviceProvider.GetService<BankingResourceRepository>(),
                Constants.Industry.Energy => _serviceProvider.GetService<EnergyResourceRepository>(),
                _ => throw new InvalidIndustryException()
            };

            if (repository == null)
            {
                throw new RepositoryException($"Resource repository could not be resolved.{nameof(ICommonRepository)}");
            }

            return repository;
        }
    }
}

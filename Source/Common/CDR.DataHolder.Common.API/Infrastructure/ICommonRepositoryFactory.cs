using CDR.DataHolder.Shared.Repository;

namespace CDR.DataHolder.Common.API.Infrastructure
{
    public interface ICommonRepositoryFactory
    {
        ICommonRepository GetCommonRepository(string industry);
    }
}

using Newtonsoft.Json.Linq;

namespace CDR.DataHolder.Shared.Repository.Infrastructure
{
    public interface IIndustryDbContext
    {
        Task RemoveExistingData();

        void ReCreateParticipants(JObject participantsData);

        Task<bool> HasExistingData();
    }
}

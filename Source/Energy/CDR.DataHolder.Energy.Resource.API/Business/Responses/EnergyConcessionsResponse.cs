using CDR.DataHolder.Energy.Resource.API.Business.Models;

namespace CDR.DataHolder.Energy.Resource.API.Business.Responses
{
    public class EnergyConcessionsResponse
    {
        public EnergyConcessions Data { get; set; } = new EnergyConcessions();

        public Shared.Business.Models.Links Links { get; set; } = new Shared.Business.Models.Links();

        public Shared.Business.Models.MetaPaginated Meta { get; set; } = new Shared.Business.Models.MetaPaginated();
    }
}

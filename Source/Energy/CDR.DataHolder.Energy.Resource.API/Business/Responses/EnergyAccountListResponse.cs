using CDR.DataHolder.Energy.Resource.API.Business.Models;

namespace CDR.DataHolder.Energy.Resource.API.Business.Responses
{
    public class EnergyAccountListResponse<T> where T : class
    {
		public Shared.Business.Models.Links Links { get; set; } = new Shared.Business.Models.Links();
		public Shared.Business.Models.MetaPaginated Meta { get; set; } = new Shared.Business.Models.MetaPaginated();
        public EnergyAccounts<T> Data { get; set; } = new EnergyAccounts<T>();
    }
}

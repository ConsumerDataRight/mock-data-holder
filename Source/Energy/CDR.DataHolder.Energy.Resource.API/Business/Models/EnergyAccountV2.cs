using Newtonsoft.Json;

namespace CDR.DataHolder.Energy.Resource.API.Business.Models
{
    public class EnergyAccountV2 : EnergyAccount
    {
        [JsonProperty(Order = 4)]
        public string OpenStatus { get; set; } = string.Empty;
    }
}

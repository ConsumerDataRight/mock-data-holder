using Newtonsoft.Json;
using System;

namespace CDR.DataHolder.Energy.Resource.API.Business.Models
{
    public class BaseEnergyAccount
    {
        [JsonProperty(Order = 1)]
        public string AccountId { get; set; } = string.Empty;

        [JsonProperty(Order = 2)]
        public string? AccountNumber { get; set; }

        [JsonProperty(Order = 3)]
        public string? DisplayName { get; set; }

        [JsonProperty(Order = 5)]
        public string CreationDate { get; set; } = string.Empty;

        [JsonProperty(Order = 6)]
        public EnergyAccountPlan[] Plans { get; set; } = Array.Empty<EnergyAccountPlan>();
    }

    public class EnergyAccount : BaseEnergyAccount
    {
    }

    public class EnergyAccountV2 : BaseEnergyAccount
    {
        [JsonProperty(Order = 4)]
        public string OpenStatus { get; set; } = string.Empty;
    }
}

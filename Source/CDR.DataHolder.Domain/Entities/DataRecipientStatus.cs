using Newtonsoft.Json;

namespace CDR.DataHolder.Domain.Entities
{
    public class DataRecipientStatus
    {
        [JsonProperty(PropertyName = "dataRecipientId")]
        public string ID { get; set; }

        [JsonProperty(PropertyName = "dataRecipientStatus")]
        public string Status { get; set; }
    }
}

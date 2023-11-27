using Newtonsoft.Json;
using System.Collections.Generic;

namespace CDR.DataHolder.Energy.Repository.Entities
{
    public class LegalEntity : Shared.Repository.Entities.LegalEntity
    {
        [JsonProperty("dataRecipientBrands")]
        public virtual ICollection<Brand>? Brands { get; set; }
    }
}

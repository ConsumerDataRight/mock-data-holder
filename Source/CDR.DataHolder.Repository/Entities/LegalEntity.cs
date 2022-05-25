using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Newtonsoft.Json;

namespace CDR.DataHolder.Repository.Entities
{
	public class LegalEntity
	{
		public LegalEntity()
		{
            this.LegalEntityId = Guid.NewGuid();
		}

        [Key]
		public Guid LegalEntityId { get; set; }

        [MaxLength(100), Required]
        public string LegalEntityName { get; set; }

        [MaxLength(25), Required]
        public string Status { get; set; }

        [MaxLength(1000)]
        public string LogoUri { get; set; }

        [JsonProperty("dataRecipientBrands")]
		public virtual ICollection<Brand> Brands { get; set; }
	}
}

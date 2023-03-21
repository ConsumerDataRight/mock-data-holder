using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Newtonsoft.Json;

namespace CDR.DataHolder.Repository.Entities
{
	public class Brand
	{
		public Brand()
		{
			this.BrandId = Guid.NewGuid();
		}

		[Key]
		[JsonProperty("dataRecipientBrandId")]
		public Guid BrandId { get; set; }
	
		[MaxLength(100), Required]
		public string BrandName { get; set; }
		
		[MaxLength(1000)]
		public string LogoUri { get; set; }

		[MaxLength(25), Required]
		public string Status { get; set; }

		public Guid LegalEntityId { get; set; }

		public LegalEntity LegalEntity { get; set; }
	}
}

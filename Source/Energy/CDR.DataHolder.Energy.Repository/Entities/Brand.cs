using System.Collections.Generic;

namespace CDR.DataHolder.Energy.Repository.Entities
{
    public class Brand : Shared.Repository.Entities.Brand
    {		
		public LegalEntity? LegalEntity { get; set; }
	}
}

using System;
using System.ComponentModel.DataAnnotations;

namespace CDR.DataHolder.Banking.Repository.Entities
{
	public class Organisation : Shared.Repository.Entities.Organisation
	{
        public virtual Customer Customer { get; set; } = new Customer();
    }
}
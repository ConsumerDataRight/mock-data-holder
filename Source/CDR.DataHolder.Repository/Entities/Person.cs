using System;
using System.ComponentModel.DataAnnotations;

namespace CDR.DataHolder.Repository.Entities
{
	public class Person
	{
		public Person()
		{
			this.PersonId = new Guid();
		}

		[Key]
		public Guid PersonId { get; set; }

		[MaxLength(100)]
		public string FirstName { get; set; }
		
		[Required, MaxLength(100)]
		public string LastName { get; set; }
		
		[MaxLength(100)]
		public string MiddleNames { get; set; }
		
		[MaxLength(50)]
		public string Prefix { get; set; }
		
		[MaxLength(50)] 
		public string Suffix { get; set; }
		
		[MaxLength(20)] 
		public string OccupationCode { get; set; }
		
		public string OccupationCodeVersion { get; set; }
		
		public DateTime? LastUpdateTime { get; set; }

		public virtual Customer Customer { get; set; }
	}
}

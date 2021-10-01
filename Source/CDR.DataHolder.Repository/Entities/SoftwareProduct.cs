using System;
using System.ComponentModel.DataAnnotations;

namespace CDR.DataHolder.Repository.Entities
{
	public class SoftwareProduct
    {
		public SoftwareProduct()
		{
            this.SoftwareProductId = Guid.NewGuid();
        }

        [Key]
        public Guid SoftwareProductId { get; set; }

        [MaxLength(100), Required]
        public string SoftwareProductName { get; set; }

        [MaxLength(1000)]
        public string SoftwareProductDesc { get; set; }

        [MaxLength(1000)]
        public string LogoUri { get; set; }

        [MaxLength(25), Required]
        public string Status { get; set; }

        public Guid BrandId { get; set; }
        public Brand Brand { get; set; }
    }
}

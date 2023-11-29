using System.ComponentModel.DataAnnotations;

namespace CDR.DataHolder.Shared.Repository.Entities
{
    public class Customer
    {
        [Key]
        public Guid CustomerId { get; set; } = Guid.NewGuid();

        [Required, MaxLength(8)]
        public string LoginId { get; set; } = string.Empty;

        public string? CustomerUType { get; set; }

        public Guid? PersonId { get; set; }

        public Guid? OrganisationId { get; set; }
    }
}

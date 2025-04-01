using System.ComponentModel.DataAnnotations;

namespace CDR.DataHolder.Shared.Repository.Entities
{
    public class Account
    {
        [Key]
        [MaxLength(100)]
        public string AccountId { get; set; } = string.Empty;

        public DateTime? CreationDate { get; set; }

        [MaxLength(100)]
        public string? OpenStatus { get; set; }

        public Guid CustomerId { get; set; }
    }
}

using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace CDR.DataHolder.Banking.Repository.Entities
{
    public class Account : Shared.Repository.Entities.Account
	{
        [Required, MaxLength(100)]
        public string DisplayName { get; set; } = string.Empty;

        public string? ProductCategory { get; set; }

        [Required, MaxLength(200)]
        public string ProductName { get; set; } = string.Empty;

        [Required, MaxLength(50)]
        public string NickName { get; set; } = string.Empty;

        [Required, MaxLength(100)]
        public string MaskedName { get; set; } = string.Empty;

        public virtual Customer Customer { get; set; } = new Customer();

        public virtual ICollection<Transaction>? Transactions { get; set; }

    }
}

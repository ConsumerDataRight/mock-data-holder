using System;

namespace CDR.DataHolder.Banking.Domain.ValueObjects
{
    public class AccountTransactionsFilter
    {
        public string AccountId { get; set; } = string.Empty;

        public DateTime? OldestTime { get; set; }

        public DateTime? NewestTime { get; set; }

        public decimal? MinAmount { get; set; }

        public decimal? MaxAmount { get; set; }

        public string? Text { get; set; }
    }
}

using System;

namespace CDR.DataHolder.Domain.ValueObjects
{
    public class AccountTransactionsFilter
    {
        public string AccountId { get; set; }

        public DateTime? OldestTime { get; set; }

        public DateTime? NewestTime { get; set; }

        public decimal? MinAmount { get; set; }

        public decimal? MaxAmount { get; set; }

        public string Text { get; set; }

        public Guid CustomerId { get; set; }
    }
}

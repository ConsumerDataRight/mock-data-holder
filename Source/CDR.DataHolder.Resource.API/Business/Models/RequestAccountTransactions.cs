using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using CDR.DataHolder.Resource.API.Business.Filters;
using Microsoft.AspNetCore.Mvc;

namespace CDR.DataHolder.Resource.API.Business.Models
{
    public class RequestAccountTransactions : IValidatableObject
    {
        [FromRoute(Name = "accountId")]
        public string AccountId { get; set; }

        [FromQuery(Name = "oldest-time")]
        public DateTime? OldestTime { get; set; }

        [FromQuery(Name = "newest-time")]
        public DateTime? NewestTime { get; set; }

        [FromQuery(Name = "min-amount")]
        public decimal? MinAmount { get; set; }

        [FromQuery(Name = "max-amount")]
        public decimal? MaxAmount { get; set; }

        [FromQuery(Name = "text")]
        public string Text { get; set; }

        [FromQuery(Name = "page")]
        [CheckPage]
        public string Page { get; set; }

        [FromQuery(Name = "page-size")]
        [CheckPageSize]
        public string PageSize { get; set; }
        

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            var results = new List<ValidationResult>();
            
            if (string.IsNullOrEmpty(this.AccountId))
                results.Add(new ValidationResult("Invalid account id.", new List<string> { "accountId" }));

            if (OldestTime.HasValue && NewestTime.HasValue && NewestTime < OldestTime)
                results.Add(new ValidationResult("Invalid time range.", new List<string> { "oldest-time", "newest-time" }));

            if (MaxAmount.HasValue && MinAmount.HasValue && MaxAmount < MinAmount)
                results.Add(new ValidationResult("Invalid amount range.", new List<string> { "min-amount", "max-amount" }));

            return results;
        }
    }
}

using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace CDR.DataHolder.Energy.Resource.API.Business.Models
{
    public class RequestAccountConcessions : IValidatableObject
    {
        [FromRoute(Name = "accountId")]
        public string AccountId { get; set; } = string.Empty;

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            var results = new List<ValidationResult>();

            if (string.IsNullOrEmpty(this.AccountId))
            {
                results.Add(new ValidationResult("Invalid account id.", new List<string> { "accountId" }));
            }

            return results;
        }
    }
}

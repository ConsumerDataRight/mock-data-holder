using CDR.DataHolder.Shared.API.Infrastructure.Models;
using CDR.DataHolder.Shared.Domain.Models;
using Newtonsoft.Json;
using System;
using System.ComponentModel.DataAnnotations;

namespace CDR.DataHolder.Shared.Resource.API.Business.Filters
{
    [AttributeUsage(AttributeTargets.Parameter | AttributeTargets.Property, AllowMultiple = false)]
    public class CheckPageAttribute : ValidationAttribute
    {
        protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
        {
            if (value == null)
            {
                return ValidationResult.Success;
            }

            if (!int.TryParse(value.ToString(), out int page) || page <= 0 || page > 1000)
            {
                return new ValidationResult(JsonConvert.SerializeObject(ResponseErrorList.InvalidField("Page parameter is out of range. Minimum page is 1, maximum page is 1000")));
            }

            return ValidationResult.Success;
        }
    }
}

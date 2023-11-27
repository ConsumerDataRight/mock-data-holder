using CDR.DataHolder.Shared.API.Infrastructure.Models;
using CDR.DataHolder.Shared.Domain.ValueObjects;
using Newtonsoft.Json;
using System;
using System.ComponentModel.DataAnnotations;

namespace CDR.DataHolder.Shared.Resource.API.Business.Filters
{
    [AttributeUsage(AttributeTargets.Parameter, AllowMultiple = false)]
	public class CheckOpenStatusAttribute : ValidationAttribute
	{
		protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
		{
			if (value == null)
			{
				return ValidationResult.Success;
			}

			if (!AccountOpenStatus.IsValid(AccountOpenStatus.Values, value.ToString() ?? string.Empty))
			{
				return new ValidationResult(JsonConvert.SerializeObject(Error.InvalidOpenStatus()));
			}

			return ValidationResult.Success;
		}
	}
}

using System;
using System.ComponentModel.DataAnnotations;
using CDR.DataHolder.API.Infrastructure.Models;
using CDR.DataHolder.Domain.ValueObjects;
using Newtonsoft.Json;

namespace CDR.DataHolder.Resource.API.Business.Filters
{
	[AttributeUsage(AttributeTargets.Parameter, AllowMultiple = false)]
	public class CheckOpenStatusAttribute : ValidationAttribute
	{
		protected override ValidationResult IsValid(object value, ValidationContext validationContext)
		{
			if (value == null)
			{
				return ValidationResult.Success;
			}

			if (!AccountOpenStatus.IsValid(AccountOpenStatus.Values, value.ToString()))
			{
				return new ValidationResult(JsonConvert.SerializeObject(Error.InvalidOpenStatus()));
			}

			return ValidationResult.Success;
		}
	}
}

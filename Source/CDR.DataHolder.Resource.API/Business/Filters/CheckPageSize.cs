using System;
using System.ComponentModel.DataAnnotations;
using CDR.DataHolder.API.Infrastructure.Models;
using Newtonsoft.Json;

namespace CDR.DataHolder.Resource.API.Business.Filters
{
	[AttributeUsage(AttributeTargets.Parameter | AttributeTargets.Property, AllowMultiple = false)]
	public class CheckPageSizeAttribute : ValidationAttribute
	{
		protected override ValidationResult IsValid(object value, ValidationContext validationContext)
		{
			if (value == null)
			{
				return ValidationResult.Success;
			}

			if (!int.TryParse(value.ToString(), out int pageSize) || pageSize <= 0)
			{
				return new ValidationResult(JsonConvert.SerializeObject(Error.InvalidPageSize()));
			}

			if (pageSize > 1000)
			{
				return new ValidationResult(JsonConvert.SerializeObject(Error.PageSizeTooLarge()));
			}

			return ValidationResult.Success;
		}
	}
}

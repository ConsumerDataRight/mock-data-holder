using System;
using System.ComponentModel.DataAnnotations;
using CDR.DataHolder.API.Infrastructure.Models;
using CDR.DataHolder.Domain.ValueObjects;
using Newtonsoft.Json;

namespace CDR.DataHolder.Resource.API.Business.Filters
{
	[AttributeUsage(AttributeTargets.Parameter, AllowMultiple = false)]
	public class CheckProductCategoryAttribute : ValidationAttribute
	{
		protected override ValidationResult IsValid(object value, ValidationContext validationContext)
		{
			if (value == null)
			{
				return ValidationResult.Success;
			}

			if (!AccountProductCategory.IsValid(AccountProductCategory.Values, value.ToString()))
			{
				return new ValidationResult(JsonConvert.SerializeObject(Error.InvalidProductCategory()));
			}

			return ValidationResult.Success;
		}
	}
}

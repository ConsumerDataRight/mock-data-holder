using System;
using System.ComponentModel.DataAnnotations;
using CDR.DataHolder.API.Infrastructure.Models;
using Newtonsoft.Json;

namespace CDR.DataHolder.Resource.API.Business.Filters
{
	[AttributeUsage(AttributeTargets.Parameter | AttributeTargets.Property, AllowMultiple = false)]
	public class CheckPageAttribute : ValidationAttribute
	{
		protected override ValidationResult IsValid(object value, ValidationContext validationContext)
		{
			if (value == null)
			{
				return ValidationResult.Success;
			}

			if (!int.TryParse(value.ToString(), out int page) || page <= 0)
			{
				return new ValidationResult(JsonConvert.SerializeObject(Error.InvalidPage()));
			}

			return ValidationResult.Success;
		}
	}
}

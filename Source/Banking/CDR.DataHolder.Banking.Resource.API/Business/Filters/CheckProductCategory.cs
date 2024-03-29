﻿using System;
using System.ComponentModel.DataAnnotations;
using CDR.DataHolder.Banking.Domain.ValueObjects;
using CDR.DataHolder.Shared.API.Infrastructure.Models;
using Newtonsoft.Json;

namespace CDR.DataHolder.Banking.Resource.API.Business.Filters
{
	[AttributeUsage(AttributeTargets.Parameter, AllowMultiple = false)]
	public class CheckProductCategoryAttribute : ValidationAttribute
	{
		protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
		{
			if (value == null)
			{
				return ValidationResult.Success;
			}

			if (!AccountProductCategory.IsValid(AccountProductCategory.Values, value.ToString() ?? string.Empty))
			{
				return new ValidationResult(JsonConvert.SerializeObject(Error.InvalidProductCategory()));
			}

			return ValidationResult.Success;
		}
	}
}

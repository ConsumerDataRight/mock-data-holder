using System;
using System.Collections.Generic;
using System.Linq;
using FluentValidation;
using FluentValidation.Validators;

namespace CDR.DataHolder.IdentityServer.Validation
{
    public static class ValidatorExtensions
    {
        public static IRuleBuilderOptions<T, string> IsInEnum<T>(this IRuleBuilder<T, string> outerRuleBuilder, params string[] validOptions)
        {
            return outerRuleBuilder.Must(s => validOptions.Contains(s)).WithMessage($"'{{PropertyName}}' has a range of values which does not include '{{PropertyValue}}'.");
        }

        public static IRuleBuilderOptions<T, TProperty> WhenExists<T, TProperty>(this IRuleBuilderOptions<T, TProperty> outerRuleBuilder)
        {
            return outerRuleBuilder.Configure(config =>
            {
                config.ApplyCondition(ctx => ctx.PropertyValue != null, ApplyConditionTo.CurrentValidator);
            });
        }

        public static IRuleBuilderOptions<T, TProperty> WhenNotEmpty<T, TProperty>(this IRuleBuilderOptions<T, TProperty> outerRuleBuilder)
        {
            return outerRuleBuilder.Configure(config =>
            {
                config.ApplyCondition(ctx => ctx.PropertyValue != null && !string.IsNullOrEmpty(ctx.PropertyValue.ToString()), ApplyConditionTo.CurrentValidator);
            });
        }

        public static IRuleBuilderOptions<T, IEnumerable<TProperty>> WhenNotEmpty<T, TProperty>(this IRuleBuilderOptions<T, IEnumerable<TProperty>> outerRuleBuilder)
        {
            return outerRuleBuilder.Configure(config =>
            {
                config.ApplyCondition(ctx => ctx.PropertyValue != null && ((IEnumerable<TProperty>)ctx.PropertyValue).Any(), ApplyConditionTo.CurrentValidator);
            });
        }

        /// <summary>
        /// Validation will always succeed, it will just execute the action specified.
        /// </summary>
        /// <typeparam name="T">Type of object being validated.</typeparam>
        /// <typeparam name="TProperty">Type of property being validated.</typeparam>
        /// <param name="ruleBuilder">The rule builder on which the validator should be defined.</param>
        /// <param name="action">A lambda expression specifying the action.</param>
        /// <returns>IRuleBuilderOptions.</returns>
        public static IRuleBuilderOptions<T, TProperty> Should<T, TProperty>(this IRuleBuilder<T, TProperty> ruleBuilder, Action<T, TProperty> action)
        {
            return ruleBuilder.Must((@object, property) =>
            {
                action?.Invoke(@object, property);
                return true;
            });
        }

        /// <summary>
        /// Validation will always succeed, it will just execute the action specified.
        /// </summary>
        /// <typeparam name="T">Type of object being validated.</typeparam>
        /// <typeparam name="TProperty">Type of property being validated.</typeparam>
        /// <param name="ruleBuilder">The rule builder on which the validator should be defined.</param>
        /// <param name="action">A lambda expression specifying the action.</param>
        /// <returns>IRuleBuilderOptions.</returns>
        public static IRuleBuilderOptions<T, TProperty> Should<T, TProperty>(this IRuleBuilder<T, TProperty> ruleBuilder, Action<T, TProperty, PropertyValidatorContext> action)
        {
            return ruleBuilder.Must((@object, property, context) =>
            {
                action?.Invoke(@object, property, context);
                return true;
            });
        }

        /// <summary>
        /// Validation will always succeed, it will just execute the action specified.
        /// </summary>
        /// <typeparam name="T">Type of object being validated.</typeparam>
        /// <typeparam name="TProperty">Type of property being validated.</typeparam>
        /// <param name="ruleBuilder">The rule builder on which the validator should be defined.</param>
        /// <param name="action">A lambda expression specifying the action.</param>
        /// <returns>IRuleBuilderOptions.</returns>
        public static IRuleBuilderOptions<T, TProperty> Should<T, TProperty>(this IRuleBuilder<T, TProperty> ruleBuilder, Action<TProperty> action)
        {
            return ruleBuilder.Must(property =>
            {
                action?.Invoke(property);

                return true;
            });
        }
    }
}
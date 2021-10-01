using System.Collections.Generic;
using System.Threading.Tasks;
using CDR.DataHolder.IdentityServer.Models;
using IdentityServer4.Models;
using IdentityServer4.Validation;
using Microsoft.AspNetCore.Http;
using static CDR.DataHolder.IdentityServer.CdsConstants;
using DA = System.ComponentModel.DataAnnotations;
using DHValidation = CDR.DataHolder.IdentityServer.Validation;

namespace CDR.DataHolder.IdentityServer.Validation
{
    public class IntrospectionRequestValidator : DHValidation.IIntrospectionRequestValidator
    {
        private List<DA.ValidationResult> _validationResults = new List<DA.ValidationResult>();

        private readonly ISecretParser _secretParser;
        private readonly ISecretValidator _secretValidator;

        public IntrospectionRequestValidator(ISecretParser secretParser, ISecretValidator secretValidator)
        {
            _secretParser = secretParser;
            _secretValidator = secretValidator;
        }

        public async Task<IEnumerable<DA.ValidationResult>> ValidateAsync(IntrospectionRequest request, HttpContext context, Client client)
        {
            // Validate grant type
            if (string.Compare(request.GrantType, IntrospectionRequestElements.AllowedGrantType, System.StringComparison.InvariantCultureIgnoreCase) != 0)
            {
                var failureResult = new DA.ValidationResult(IntrospectionErrorCodes.UnsupportedGrantType, new List<string> { IntrospectionRequestElements.GrantType });
                _validationResults.Add(failureResult);
            }

            // Validate client assertion type
            if (string.Compare(request.ClientAssertionType, IntrospectionRequestElements.AllowedClientAssertionType, System.StringComparison.InvariantCultureIgnoreCase) != 0)
            {
                var failureResult = new DA.ValidationResult(IntrospectionErrorCodes.InvalidClient, new List<string> { IntrospectionRequestElements.ClientAssertionType });
                _validationResults.Add(failureResult);
            }

            // Parse secret
            var secretParserResult = await _secretParser.ParseAsync(context);

            // Validate client assertion
            var secretValidatorResult = await _secretValidator.ValidateAsync(client.ClientSecrets, secretParserResult);

            if (!secretValidatorResult.Success)
            {
                var failureResult = new DA.ValidationResult(secretValidatorResult.Error, new List<string> { IntrospectionRequestElements.ClientAssertion });
                _validationResults.Add(failureResult);
            }

            return _validationResults;
        }

        public async Task<IEnumerable<DA.ValidationResult>> ValidateClientAssertionAsync(HttpContext context, Client client)
        {
            // Parse secret
            var secretParserResult = await _secretParser.ParseAsync(context);

            // Validate client assertion
            var secretValidatorResult = await _secretValidator.ValidateAsync(client.ClientSecrets, secretParserResult);
            if (!secretValidatorResult.Success)
            {
                var failureResult = new DA.ValidationResult(secretValidatorResult.Error, new List<string> { IntrospectionRequestElements.ClientAssertion });
                _validationResults.Add(failureResult);
            }
            return _validationResults;
        }
    }
}
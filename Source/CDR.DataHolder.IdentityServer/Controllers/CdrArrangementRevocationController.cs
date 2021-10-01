using CDR.DataHolder.Domain.Repositories;
using CDR.DataHolder.IdentityServer.Events;
using CDR.DataHolder.IdentityServer.Interfaces;
using CDR.DataHolder.IdentityServer.Models;
using CDR.DataHolder.IdentityServer.Services;
using IdentityServer4.Models;
using IdentityServer4.Services;
using IdentityServer4.Validation;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System.Linq;
using System.Threading.Tasks;
using static CDR.DataHolder.IdentityServer.CdsConstants;

namespace CDR.DataHolder.IdentityServer.Controllers
{
    [ApiController]
    [ApiVersion("1.0")]
    [ApiVersion("2.0")]
    public class CdrArrangementRevocationController : Controller
    {
        private readonly ILogger _logger;
        private readonly IEventService _eventService;
        private readonly ICustomGrantService _customGrantService;
        private readonly ISecretParser _secretParser;
        private readonly ISecretValidator _secretValidator;
        private readonly IClientService _clientService;
		private readonly IIdSvrRepository _idSvrRepository;

		public CdrArrangementRevocationController(
            ILogger<CdrArrangementRevocationController> logger,
            IEventService eventService,
            ICustomGrantService customGrantService,
            ISecretParser secretParser,
            ISecretValidator secretValidator,
            IClientService clientService,
            IIdSvrRepository idSvrRepository)
        {
            _eventService = eventService;
            _logger = logger;
            _customGrantService = customGrantService;
            _secretParser = secretParser;
            _secretValidator = secretValidator;
            _clientService = clientService;
			_idSvrRepository = idSvrRepository;
        }

        [HttpPost]
        [Route("connect/arrangements/revoke")]
        [Consumes("application/x-www-form-urlencoded")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> ArrangementRevocation()
        {
            var secretParserResult = await _secretParser.ParseAsync(HttpContext);

            var clientArrangeRevokeRequest = GetClientArrangementRevocationRequest(secretParserResult);
            if (clientArrangeRevokeRequest == null)
            {
                _logger.LogError("Request invalid, or missing required params or headers");
                return await ReturnErrorResponseAndLogEvent(ValidationCheck.CdrArrangementRevocationInvalidRequest, new BadRequestResult());
            }

            var client = await _clientService.FindClientById(clientArrangeRevokeRequest.ClientDetails.ClientId);
            if (client == null)
            {
                _logger.LogError("Invalid clientid provided in request, no matching client found in Auth Server {ClientId}", clientArrangeRevokeRequest.ClientDetails.ClientId);
                return await ReturnErrorResponseAndLogEvent(ValidationCheck.CdrArrangementRevocationInvalidClientId, Unauthorized(null));
            }

            // Check the software product id status.
            var softwareProductId = client.Claims.FirstOrDefault(c => c.Type == ClientMetadata.SoftwareId)?.Value;
            var softwareProduct = await _idSvrRepository.GetSoftwareProduct(System.Guid.Parse(softwareProductId));
            if (softwareProduct != null && softwareProduct.Status == "REMOVED")
            {
                _logger.LogError("Software Product ID {SoftwareProductId} is REMOVED", softwareProduct.SoftwareProductId);
                return await ReturnErrorResponseAndLogEvent(ValidationCheck.CdrArrangementRevocationInvalidClientId, new UnauthorizedResult());
            }

            var secretValidatorResult = await _secretValidator.ValidateAsync(client.ClientSecrets, secretParserResult);

            if (secretValidatorResult.Success)
            {
                int revokeGrantResult = await _customGrantService.RemoveGrantsForCdrArrangementId(clientArrangeRevokeRequest.CdrArrangementId, client.ClientId);
                if (revokeGrantResult == 1)
                {
                    await _customGrantService.RemoveGrant(clientArrangeRevokeRequest.CdrArrangementId);
                    await _eventService.RaiseAsync(new CdrArrangementRevocationValidationSuccessEvent());

                    if (clientArrangeRevokeRequest.GrantType == null)
                    {
                        return NoContent();
                    }
                    else
                    {
                        string grantType = client.AllowedGrantTypes.FirstOrDefault(grant => grant.Contains(clientArrangeRevokeRequest.GrantType));
                        if (string.IsNullOrEmpty(grantType))
                            return Unauthorized(null);
                        else
                            return NoContent();
                    }
                }
                else if (revokeGrantResult == 2)
                {
                    await _eventService.RaiseAsync(new CdrArrangementRevocationValidationFailureEvent(ValidationCheck.CdrArrangementRevocationInvalidCDRArrangementId));
                    string errMsg = @"{""errors"":[{""code"":""urn:au-cds:error:cds-all:Authorisation/InvalidArrangement"",""title"":""Invalid Consent Arrangement"",""detail"":""CDR arrangement ID is not valid"",""meta"":{}}]}";
                    return UnprocessableEntity(errMsg);
                }
                else if (revokeGrantResult == 3)
                {
                    await _eventService.RaiseAsync(new CdrArrangementRevocationValidationFailureEvent(ValidationCheck.CdrArrangementRevocationInvalidCDRArrangementId));
                    string errMsg = @"{""errors"":[{""code"":""urn:au-cds:error:cds-all:Authorisation/InvalidArrangement"",""title"":""Invalid Consent Arrangement"",""detail"":""CDR Arrangement ID is not valid for the given client"",""meta"":{}}]}";
                    return UnprocessableEntity(errMsg);
                }
                else
                {
                    return BadRequest();
                }
            }
            else
            {
                _logger.LogError("Error returned from secretValidator {@SecretValidatorResult}", secretValidatorResult);

                if (secretValidatorResult.Error == CdrArrangementRevocationErrorCodes.InvalidClient)
                {
                    return Unauthorized(null);
                }

                return BadRequest();
            }
        }

        private async Task<ActionResult> ReturnErrorResponseAndLogEvent(ValidationCheck check, ActionResult result)
        {
            await _eventService.RaiseAsync(new CdrArrangementRevocationValidationFailureEvent(check));
            return result;
        }

        private ClientArrangementRevocationRequest GetClientArrangementRevocationRequest(ParsedSecret secret)
        {
            if (secret.Credential != null)
            {
                var clientArrangementRevocationRequest = (ClientArrangementRevocationRequest)secret.Credential;
                if (clientArrangementRevocationRequest != null)
                {
                    return clientArrangementRevocationRequest;
                }
            }

            return null;
        }
    }
}
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Threading.Tasks;
using CDR.DataHolder.IdentityServer.Configuration;
using CDR.DataHolder.IdentityServer.Events;
using CDR.DataHolder.IdentityServer.Extensions;
using CDR.DataHolder.IdentityServer.Interfaces;
using CDR.DataHolder.IdentityServer.Models;
using IdentityServer4.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using static CDR.DataHolder.IdentityServer.CdsConstants;

namespace CDR.DataHolder.IdentityServer.Controllers
{
    // https://tools.ietf.org/html/draft-ietf-oauth-par-01
    [ApiController]
    [ApiVersion("1.0")]
    [ApiVersion("2.0")]
    public class PushedAuthorizationRequestController : Controller
    {
        private readonly ILogger _logger;
        private readonly IEventService _eventService;
        private readonly IPushedAuthorizationRequestService _pushAuthoriseRequestService;
        private readonly IConfigurationSettings _configurationSettings;

        public PushedAuthorizationRequestController(
            ILogger<PushedAuthorizationRequestController> logger,
            IPushedAuthorizationRequestService pushAuthoriseRequestService,
            IEventService eventService,
            IConfigurationSettings configurationSettings)
        {
            _eventService = eventService;
            _logger = logger;
            _pushAuthoriseRequestService = pushAuthoriseRequestService;
            _configurationSettings = configurationSettings;
        }

        [HttpPost]
        [Route("connect/par")]
        [Consumes("application/x-www-form-urlencoded")]
        [Produces("application/json")]
        [ProducesResponseType(typeof(PushedAuthorizationCreatedResponse), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(PushedAuthorizationErrorResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(PushedAuthorizationErrorResponse), StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> Post()
        {
            if (!HttpContext.Request.HasFormContentType)
            {
                await RaiseEventAndLogFailedValidation("PAR Endpoint requires Form content", ValidationCheck.PARMissingFormBody);
                return new UnsupportedMediaTypeResult();
            }

            NameValueCollection values = HttpContext.Request.Form.AsNameValueCollection();

            var parResultResponse = await _pushAuthoriseRequestService.ProcessAuthoriseRequest(values);

            if (!parResultResponse.HasError)
            {
                var parCreatedResponse = new PushedAuthorizationCreatedResponse()
                {
                    RequestUri = parResultResponse.RequestUri,
                    ExpiresIn = _configurationSettings.ParRequestUriExpirySeconds,
                };

                await _eventService.RaiseAsync(new PushedAuthorizationRequestValidationSuccessEvent());

                return CreatedAtAction(nameof(Post), parCreatedResponse);
            }
            else
            {
                return ReturnErrorResponseFromService(parResultResponse);
            }
        }

        private static IActionResult ReturnErrorResponseFromService(PushedAuthorizationResult result)
        {
            if (result.Error == PushedAuthorizationServiceErrorCodes.RequestJwtFailedValidation
                || result.Error == PushedAuthorizationServiceErrorCodes.UnauthorizedClient)
            {
                // Sends back 401 if unauthorized client or request jwt fails token validation
                // https://tools.ietf.org/html/draft-ietf-oauth-par-01#section-3.1.1
                return new UnauthorizedObjectResult(new PushedAuthorizationErrorResponse()
                {
                    Error = AuthorizeErrorCodes.UnauthorizedClient,
                    Description = result.ErrorDescription,
                });
            }
            else if (PushedAuthorizationResponseErrorCodes.Contains(result.Error))
            {
                return new BadRequestObjectResult(new PushedAuthorizationErrorResponse()
                {
                    Error = result.Error,
                    Description = result.ErrorDescription,
                });
            }

            return new BadRequestObjectResult(new PushedAuthorizationBadRequestErrorResponse()
            {
                Errors = new List<Error>
                {
                    new Error
                    {
                        Code = "urn:au-cds:error:cds-all:Field/Invalid",
                        Title = "Invalid Field",
                        Detail = "cdr_arrangement_id is invalid",
                        Meta = new Meta()
                    }
                }
            });
        }

        private async Task RaiseEventAndLogFailedValidation(string error, ValidationCheck check)
        {
            await _eventService.RaiseAsync(new PushedAuthorizationRequestValidationFailureEvent(check));
            _logger.LogError(error);
        }
    }
}
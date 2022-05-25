using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using CDR.DataHolder.API.Infrastructure.Authorization;
using CDR.DataHolder.API.Infrastructure.Filters;
using CDR.DataHolder.API.Infrastructure.Models;
using CDR.DataHolder.IdentityServer.Events;
using CDR.DataHolder.IdentityServer.Models;
using CDR.DataHolder.IdentityServer.Services;
using CDR.DataHolder.IdentityServer.Validation;
using IdentityModel;
using IdentityServer4.Models;
using IdentityServer4.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Net.Http.Headers;
using Serilog.Context;
using static CDR.DataHolder.IdentityServer.CdsConstants;

namespace CDR.DataHolder.IdentityServer.Controllers
{
    [ApiController]
    [ApiVersion("1.0")]
    [ApiVersion("2.0")]
    public class ClientRegistrationController : ControllerBase
    {
        private readonly IClientService _clientService;
        private readonly IMapper _mapper;
        private readonly IClientRegistrationRequestValidator _validator;
        private readonly ILogger _logger;
        private readonly IEventService _eventService;
        private readonly IConfiguration _config;

        public ClientRegistrationController(
            ILogger<ClientRegistrationController> logger,
            IClientService clientService,
            IMapper mapper,
            IClientRegistrationRequestValidator validator,
            IConfiguration config,
            IEventService eventService)
        {
            _clientService = clientService;
            _mapper = mapper;
            _validator = validator;
            _logger = logger;
            _config = config;
            _eventService = eventService;
        }

        /// <summary>
        /// Register a client using a CDR Register issued Software Statement Assertion.
        /// </summary>
        /// <param name="request">The registration request JWT, as defined in [Dynamic Client Registration](https://cdr-register.github.io/register/#dynamic-client-registration), to be used to register with a Data Holder.</param>
        /// <response code="201">Client registration success.</response>
        /// <response code="400">Request failed due to client error.</response>
        [HttpPost]
        [Route("connect/register")]
        [Consumes("application/jwt")]
        [Produces("application/json")]
        [ProducesResponseType(typeof(ClientRegistrationResponse), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(ClientRegistrationError), StatusCodes.Status400BadRequest)]
        [ServiceFilter(typeof(LogActionEntryAttribute))]
        public async Task<IActionResult> Post(ClientRegistrationRequest request)
        {
            // Validate the registration request.
            var (isValid, invalidRequestResponse) = await ValidateRequest(request);
            if (!isValid)
            {
                return invalidRequestResponse;
            }

            // Check if the software product has already been registered.
            var existingClient = await _clientService.FindClientBySoftwareProductId(request.SoftwareStatement.SoftwareId);
            if (existingClient != null)
            {
                return new BadRequestObjectResult(new ClientRegistrationError("invalid_client_metadata", "Duplicate registrations for a given software_id are not valid."));
            }

            using (LogContext.PushProperty("MethodName", ControllerContext.RouteData.Values["action"].ToString()))
            {
                _logger.LogInformation("RegistrationRequest: {@registrationRequest}", request);
            }

            var response = _mapper.Map<ClientRegistrationResponse>(request);

            using (LogContext.PushProperty("MethodName", ControllerContext.RouteData.Values["action"].ToString()))
            {
                _logger.LogInformation("RegistrationResponse: {@registrationResponse}", response);
            }

            var client = _mapper.Map<DataRecipientClient>(response);
            await _clientService.RegisterClient(client);
            return CreatedAtAction(nameof(Post), EnsureResponse(response));
        }

        /// <summary>
        /// Update a client using a CDR Register issued Software Statement Assertion.
        /// </summary>
        /// <param name="request">The registration request JWT, as defined in [Dynamic Client Registration](https://cdr-register.github.io/register/#dynamic-client-registration), to be used to register with a Data Holder.</param>
        /// <response code="200">Client registration update success.</response>
        /// <response code="400">Request failed due to client error.</response>
        [HttpPut]
        [Route("connect/register/{clientId}")]
        [Consumes("application/jwt")]
        [Produces("application/json")]
        [ProducesResponseType(typeof(ClientRegistrationResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ClientRegistrationError), StatusCodes.Status400BadRequest)]
        [PolicyAuthorize(AuthorisationPolicy.DynamicClientRegistration)]
        [ServiceFilter(typeof(LogActionEntryAttribute))]
        public async Task<IActionResult> Put([Required] string clientId, IClientRegistrationRequest request)
        {
            using (LogContext.PushProperty("MethodName", ControllerContext.RouteData.Values["action"].ToString()))
            {
                _logger.LogInformation("Put Registration Request: {@clientId}", clientId);
                _logger.LogInformation("RegistrationRequest: {@registrationRequest}", request);
            }

            // Validate the incoming client id.
            var (isValidClientId, client) = await ValidateClient(clientId);
            if (!isValidClientId)
            {
                return InvalidClientIdResult();
            }

            // Validate the incoming request.
            var (isValid, invalidRequestResponse) = await ValidateRequest(request);
            if (!isValid)
            {
                return invalidRequestResponse;
            }

            var response = _mapper.Map<ClientRegistrationResponse>(request);

            // Ensure that the same client id and client id issued at values are kept between updates.
            response.ClientId = clientId;
            response.ClientIdIssuedAt = Convert.ToInt64(client.Claims.First(x => x.Type == RegistrationResponse.ClientIdIssuedAt).Value);

            using (LogContext.PushProperty("MethodName", ControllerContext.RouteData.Values["action"].ToString()))
            {
                _logger.LogInformation("RegistrationResponse: {@registrationResponse}", response);
            }

            var drClient = _mapper.Map<DataRecipientClient>(response);
            await _clientService.UpdateClient(drClient);
            return new OkObjectResult(EnsureResponse(response));
        }

        /// <summary>
        /// Get a client using client id.
        /// </summary>
        /// <param name="clientId">The client id</param>
        /// <response code="200">Client registration update success.</response>
        /// <response code="400">Request failed due to client error.</response>
        [HttpGet]
        [Route("connect/register/{clientId}")]
        [Produces("application/json")]
        [ProducesResponseType(typeof(ClientRegistrationResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [PolicyAuthorize(AuthorisationPolicy.DynamicClientRegistration)]
        [ServiceFilter(typeof(LogActionEntryAttribute))]
        public async Task<IActionResult> Get([Required] string clientId)
        {
            using (LogContext.PushProperty("MethodName", ControllerContext.RouteData.Values["action"].ToString()))
            {
                _logger.LogInformation("Get Registration Request: {@clientId}", clientId);
            }

            // Validate the incoming client id.
            var (isValid, client) = await ValidateClient(clientId);
            if (!isValid)
            {
                return InvalidClientIdResult();
            }

            var dataRecipientClient = _mapper.Map<DataRecipientClient>(client);
            var response = _mapper.Map<ClientRegistrationResponse>(dataRecipientClient);

            using (LogContext.PushProperty("MethodName", ControllerContext.RouteData.Values["action"].ToString()))
            {
                _logger.LogInformation("RegistrationResponse: {@registrationResponse}", response);
            }

            return new OkObjectResult(EnsureResponse(response));
        }

        /// <summary>
        /// Delete a client using client id.
        /// </summary>
        /// <param name="clientId">The client id</param>
        /// <response code="204">No content.</response>
        /// <response code="400">Request failed due to client error.</response>
        [HttpDelete]
        [Route("connect/register/{clientId}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(typeof(ClientRegistrationError), StatusCodes.Status404NotFound)]
        [PolicyAuthorize(AuthorisationPolicy.DynamicClientRegistration)]
        [ServiceFilter(typeof(LogActionEntryAttribute))]
        public async Task<IActionResult> Delete([Required] string clientId)
        {
            using (LogContext.PushProperty("MethodName", ControllerContext.RouteData.Values["action"].ToString()))
            {
                _logger.LogInformation("Delete Registration Request: {@clientId}", clientId);
            }

            // Validate the incoming client id.
            var (isValid, client) = await ValidateClient(clientId);
            if (!isValid)
            {
                return InvalidClientIdResult();
            }

            await _clientService.RemoveClientById(clientId);

            using (LogContext.PushProperty("MethodName", ControllerContext.RouteData.Values["action"].ToString()))
            {
                _logger.LogInformation("Client deleted: {@clientId}", clientId);
            }

            return new NoContentResult();
        }

        private async Task<(bool, IActionResult)> ValidateRequest(IClientRegistrationRequest request)
        {
            /*
                Jwt input formatter returns null when it cannot parse the request body or cannot parse software statetment within the request body
                When the main request body can be parsed, but not software statement, it adds validation error to ModelState.
                The rules are:
                    When the whole request body cannot be parsed we return 415 (UnsupportedMediaType)
                    When the request body can be parsed but software statemant cannot be parsed we return 400 (BadRequest)
            */
            if (request == null && ModelState.IsValid)
            {
                return (false, await UnsupportedMediaTypeResponse());
            }

            if (ModelState.IsValid)
            {
                var validationResults = await _validator.ValidateAsync(request);

                if (validationResults.Any())
                {
                    return (false, BadRequestResponse(validationResults));
                }
            }

            await _eventService.RaiseAsync(new ClientAssertionSuccessEvent());
            await _eventService.RaiseAsync(new RegistrationValidationSuccessEvent());

            return (true, null);
        }

        private static IActionResult BadRequestResponse(IEnumerable<ValidationResult> validationResults)
        {
            // SSA errors.
            var ssaErrors = validationResults.Where(x => String.Join(',', x.MemberNames) == nameof(ClientRegistrationRequest.SoftwareStatement));
            if (ssaErrors.Any())
            {
                return new BadRequestObjectResult(new ClientRegistrationError(RegistrationErrors.InvalidSoftwareStatement, ssaErrors.First().ErrorMessage));
            }

            // SSA Signature Errors.
            var ssaSigErrors = validationResults.Where(x => String.Join(',', x.MemberNames) == "SoftwareStatement.Signature");
            if (ssaSigErrors.Any())
            {
                return new BadRequestObjectResult(new ClientRegistrationError(RegistrationErrors.UnapprovedSoftwareStatement, "Software statement is not approved by register"));
            }

            // Redirect errors.
            var redirectUrisErrors = validationResults.Where(x => String.Join(',', x.MemberNames) == nameof(ClientRegistrationRequest.RedirectUris));
            if (redirectUrisErrors.Any())
            {
                return new BadRequestObjectResult(new ClientRegistrationError(RegistrationErrors.InvalidRedirectUri, redirectUrisErrors.First().ErrorMessage));
            }

            // Return the first client metadata error.
            return new BadRequestObjectResult(new ClientRegistrationError(RegistrationErrors.InvalidClientMetadata, validationResults.First().ErrorMessage));
        }

        private async Task<IActionResult> UnsupportedMediaTypeResponse()
        {
            const string errorMessage = "Unsupported Media Type";
            await RaiseRegistrationValidationFailureEvent(ValidationCheck.RegistrationRequestUnsupportedMediaType, errorMessage);
            return new UnsupportedMediaTypeResult();
        }

        private async Task RaiseRegistrationValidationFailureEvent(ValidationCheck check, string message = null)
        {
            using (LogContext.PushProperty("MethodName", "RaiseRegistrationValidationFailureEvent"))
            {
                _logger.LogError("{message}", message);
            }
            await _eventService.RaiseAsync(new RegistrationValidationFailureEvent(check, message));
        }

        /// <summary>
        /// Ensure that the response returned maps to the capabilities supported by the Data Holder.
        /// </summary>
        /// <param name="response">ClientRegistrationResponse</param>
        /// <returns></returns>
        private ClientRegistrationResponse EnsureResponse(ClientRegistrationResponse response)
        {
            // Merge the scopes.
            var clientScope = response.Scope.Split(' ');
            var serverScope = _config["ScopesSupported"].Split(',');
            response.Scope = String.Join(' ', clientScope.Intersect(serverScope));

            if (string.IsNullOrEmpty(response.ApplicationType))
            {
                response.ApplicationType = "web";
            }

            if (string.IsNullOrEmpty(response.RequestObjectSigningAlg))
            {
                response.RequestObjectSigningAlg = "PS256";
            }

            return response;
        }

        private async Task<(bool isValid, Client client)> ValidateClient(string clientId)
        {
            // Check that the provided client id matches the client_id in the access token.
            var clientIdClaimValue = this.User.Claims.FirstOrDefault(c => c.Type == JwtClaimTypes.ClientId);

            if (clientIdClaimValue == null || !clientIdClaimValue.Value.Equals(clientId))
            {
                using (LogContext.PushProperty("MethodName", "ValidateClient"))
                {
                    _logger.LogInformation("Client ID does not match access token: clientId = {clientId}", clientId);
                }
                return (false, null);
            }

            // Lookup the client in the repository.
            var client = await _clientService.FindClientById(clientId);
            if (client == null)
            {
                using (LogContext.PushProperty("MethodName", "ValidateClient"))
                {
                    _logger.LogInformation("Client not found: {clientId}", clientId);
                }
                return (false, null);
            }

            return (true, client);
        }

        private IActionResult InvalidClientIdResult()
        {
            Response.Headers.Append(
                HeaderNames.WWWAuthenticate,
                $"Bearer error=\"{AuthorizeErrorCodes.InvalidRequest}\", error_description=\"{AuthorizeErrorCodeDescription.UnknownClientRegistration}\"");
            return Unauthorized();
        }
    }
}
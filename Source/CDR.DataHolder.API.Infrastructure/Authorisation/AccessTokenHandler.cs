using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Primitives;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace CDR.DataHolder.API.Infrastructure.Authorisation
{
    public class AccessTokenHandler : AuthorizationHandler<AccessTokenRequirement>
    {
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IConfiguration _config;
        private readonly ILogger<AccessTokenHandler> _logger;

        public AccessTokenHandler(IHttpContextAccessor httpContextAccessor, IConfiguration config, ILogger<AccessTokenHandler> logger)
        {
            _httpContextAccessor = httpContextAccessor;
            _config = config;
            _logger = logger;
        }

        protected override async Task HandleRequirementAsync(AuthorizationHandlerContext context, AccessTokenRequirement requirement)
        {
            // Check that authentication was successful before doing anything else
            if (!context.User.Identity.IsAuthenticated)
            {
                return;
            }

            // Check if the access token has been revoked.
            _logger.LogInformation($"{nameof(AccessTokenHandler)}.{nameof(HandleRequirementAsync)} - Checking the access token...");

            // Call the Mock Data Holder's idp to introspect the access token.
            var success = await CheckAccessToken();

            if (success)
            {
                _httpContextAccessor.HttpContext.Items["ValidAccessToken"] = true;
                context.Succeed(requirement);
            }
            else
            {
                _logger.LogError($"{nameof(AccessTokenHandler)}.{nameof(HandleRequirementAsync)} check failed.");
                _httpContextAccessor.HttpContext.Items["ValidAccessToken"] = false;
                context.Fail();
            }
        }

        private async Task<bool> CheckAccessToken()
        {
            // Get the Authorization header value.
            StringValues authHeader;
            if (!_httpContextAccessor.HttpContext.Request.Headers.TryGetValue("Authorization", out authHeader))
            {
                _logger.LogError($"Authorization header not found on HTTP request.");
                return false;
            }

            if (!authHeader.ToString().StartsWith("Bearer "))
            {
                _logger.LogError($"Authorization header does not contain Bearer token.");
                return false;
            }

            // Introspect the access token.
            var accessToken = authHeader.ToString().Replace("Bearer ", "");
            var endpoint = _config["AccessTokenIntrospectionEndpoint"];

            var handler = new HttpClientHandler
            {
                ServerCertificateCustomValidationCallback = (a, b, c, d) => true
            };
            var httpClient = new HttpClient(handler);

            var formFields = new List<KeyValuePair<string, string>>();
            formFields.Add(new KeyValuePair<string, string>("token", accessToken));

            var response = await httpClient.PostAsync(endpoint, new FormUrlEncodedContent(formFields));

            if (response.IsSuccessStatusCode)
            {
                var body = await response.Content.ReadAsStringAsync();
                var json = JsonConvert.DeserializeObject<JObject>(body);
                return json.GetValue("active").Value<bool>();
            }
            else
            {
                return false;
            }
        }
    }
}

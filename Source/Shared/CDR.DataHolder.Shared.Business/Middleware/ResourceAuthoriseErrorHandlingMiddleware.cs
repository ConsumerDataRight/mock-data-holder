using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Primitives;
using System.IdentityModel.Tokens.Jwt;
using Infra = CDR.DataHolder.Shared.API.Infrastructure;

namespace CDR.DataHolder.Shared.Business.Middleware
{
    public class ResourceAuthoriseErrorHandlingMiddleware : IMiddleware
    {
        private readonly ILogger<ResourceAuthoriseErrorHandlingMiddleware> _logger;

        public ResourceAuthoriseErrorHandlingMiddleware(
            ILogger<ResourceAuthoriseErrorHandlingMiddleware> logger)
        {
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context, RequestDelegate next)
        {
            await next.Invoke(context);

            try
            {
                if (context.Request.Path.ToString().EndsWith(Constants.ResourceEndPoints.GetAccounts) && context.Response.StatusCode == StatusCodes.Status401Unauthorized)
                {
                    try
                    {
                        context.Request.Headers.TryGetValue("Authorization", out StringValues authHeader);
                        var accessToken = authHeader.ToString().Replace("Bearer ", string.Empty);
                        var jwt = accessToken;

                        // Try to get the token. Will fail if the token is invalid
                        var handler = new JwtSecurityTokenHandler();
                        handler.ReadJwtToken(jwt);
                    }
                    catch
                    {
                        // Token creation failed. Set error message to invalid_token
                        await SetUnauthorisedErrorResponseAsync(context, Infra.Constants.UnauthorisedErrors.InvalidToken);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing the Resource Middleware {ExceptionMessage} {StackTrace}", ex.Message, ex.StackTrace);
            }
        }

        private static async Task SetUnauthorisedErrorResponseAsync(HttpContext httpContext, string error)
        {
            // Replace the response body with the custom error message
            var originBody = httpContext.Response.Body;

            var memStream = new MemoryStream();
            httpContext.Response.Body = memStream;
            memStream.Position = 0;

            // Get the custom error message
            var responseBody = Infra.Constants.UnauthorisedErrors.ErrorMessage.Replace(Infra.Constants.UnauthorisedErrors.ErrorMessageDetailReplace, error);

            var memoryStreamModified = new MemoryStream();
            var sw = new StreamWriter(memoryStreamModified);
            await sw.WriteAsync(responseBody);
            await sw.FlushAsync();
            memoryStreamModified.Position = 0;

            await memoryStreamModified.CopyToAsync(originBody).ConfigureAwait(false);
        }
    }
}

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace CDR.DataHolder.Admin.API.Models
{
    public class AuthorizationResult
    {
        public bool IsAuthorized { get; set; }

        public string? Error { get; set; }

        public string? ErrorDescription { get; set; }

        public IActionResult SendError(HttpResponse response)
        {
            response.Headers.Append("WWW-Authenticate", $"Bearer error=\"{this.Error}\"");
            return new UnauthorizedObjectResult(new { error = this.Error, error_description = this.ErrorDescription });
        }

        public static AuthorizationResult Fail(string error, string errorDescription)
        {
            return new AuthorizationResult()
            {
                IsAuthorized = false,
                Error = error,
                ErrorDescription = errorDescription,
            };
        }

        public static AuthorizationResult Pass()
        {
            return new AuthorizationResult()
            {
                IsAuthorized = true,
            };
        }
    }
}

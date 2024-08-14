using System.Net;
using CDR.DataHolder.Shared.Domain.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Versioning;

namespace CDR.DataHolder.Shared.API.Infrastructure.Models
{
    public class ErrorResponseVersion : DefaultErrorResponseProvider
    {
        public override IActionResult CreateResponse(ErrorResponseContext context)
        {
            // The version was not specified.
            if (context.ErrorCode == "ApiVersionUnspecified")
            {
                return new ObjectResult(new ResponseErrorList().AddMissingRequiredHeader("x-v")) //TODO: This isn't consistent with the new PT behaviour (or RAAP)
                {
                    StatusCode = (int)HttpStatusCode.BadRequest
                };
            }

            // Get x-v from request header
            var versionHeaderValue = context.Request.Headers["x-v"];
            var invalid_XV_Version = true;

            // If the x-v is set, check that it is a postive integer.
            if (int.TryParse(versionHeaderValue, out int version))
            {
                invalid_XV_Version = version < 1;
            }

            if (invalid_XV_Version)
            {
                return new ObjectResult(new ResponseErrorList().AddInvalidXVInvalidVersion())
                {
                    StatusCode = (int)HttpStatusCode.BadRequest
                };
            }

            if (context.ErrorCode == "InvalidApiVersion")
            {
                return new ObjectResult(new ResponseErrorList().AddInvalidXVInvalidVersion())
                {
                    StatusCode = (int)HttpStatusCode.BadRequest
                };
            }

            if (context.ErrorCode == "UnsupportedApiVersion")
            {
                return new ObjectResult(new ResponseErrorList().AddInvalidXVUnsupportedVersion())
                {
                    StatusCode = (int)HttpStatusCode.NotAcceptable
                };
            }

            return base.CreateResponse(context);
        }
    }
}

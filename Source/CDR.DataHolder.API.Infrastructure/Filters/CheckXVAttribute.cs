using System;
using System.Net;
using CDR.DataHolder.API.Infrastructure.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace CDR.DataHolder.Resource.API.Infrastructure.Filters
{
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = false)]
    public class CheckXVAttribute : ActionFilterAttribute
    {
        private readonly int _minVersion;
        private readonly int _maxVersion;

        public CheckXVAttribute(int minVersion, int maxVersion)
        {
            _minVersion = minVersion;
            _maxVersion = maxVersion;
        }

        public override void OnActionExecuting(ActionExecutingContext context)
        {
            // Get x-v from request header
            var versionHeaderValue = context.HttpContext.Request.Headers["x-v"];

            if (string.IsNullOrEmpty(versionHeaderValue))
            {
                context.Result = new BadRequestObjectResult(new ResponseErrorList(Error.MissingHeader("x-v")));
            }
            else
            {
                // If the x-v is set, check that it is a postive integer.
                if (int.TryParse(versionHeaderValue, out int version) && version > 0)
                {
                    if (version < _minVersion || version > _maxVersion)
                    {
                        // return a 406 Not Accepted as the version is not supported.
                        context.Result = new ObjectResult(new ResponseErrorList(Error.UnsupportedVersion()))
                        {
                            StatusCode = (int)HttpStatusCode.NotAcceptable
                        };
                    }
                }
                else
                {
                    // Return a 400 bad request.
                    context.Result = new BadRequestObjectResult(new ResponseErrorList(Error.InvalidVersion()));
                }
            }

            base.OnActionExecuting(context);
        }

        public override void OnActionExecuted(ActionExecutedContext context)
        {
            // Set version (x-v) we are responding with in the response header
            context.HttpContext.Response.Headers["x-v"] = _maxVersion.ToString();

            base.OnActionExecuted(context);
        }
    }
}
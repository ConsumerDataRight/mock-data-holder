using System;
using Microsoft.AspNetCore.Mvc.Filters;

namespace CDR.DataHolder.Shared.API.Infrastructure.Filters
{
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = false)]
    public class SetVersionResponseAttribute : ActionFilterAttribute
    {
        private readonly int _version;

        public SetVersionResponseAttribute(int version)
        {
            _version = version;
        }

        public override void OnActionExecuted(ActionExecutedContext context)
        {
            // Set version (x-v) we are responding with in the response header
            context.HttpContext.Response.Headers["x-v"] = _version.ToString();

            base.OnActionExecuted(context);
        }
    }
}

using CDR.DataHolder.API.Infrastructure.Models;
using CDR.DataHolder.API.Infrastructure.Versioning;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace CDR.DataHolder.API.Infrastructure.Middleware
{
    public static class ApiExceptionHandler
    {
        public async static Task Handle(HttpContext context)
        {
            var exceptionDetails = context.Features.Get<IExceptionHandlerFeature>();
            var ex = exceptionDetails?.Error;

            if (ex != null)
            {
                var handledError = string.Empty;
                var statusCode = (int)HttpStatusCode.BadRequest;
                var jsonSerializerSettings = new JsonSerializerSettings()
                {
                    Formatting = Formatting.Indented,
                    NullValueHandling = NullValueHandling.Ignore,
                    ContractResolver = new DefaultContractResolver
                    {
                        NamingStrategy = new CamelCaseNamingStrategy()
                    }
                };

                if (ex is InvalidVersionException)
                {
                    statusCode = (int)HttpStatusCode.BadRequest;
                    handledError = JsonConvert.SerializeObject(new ResponseErrorList(Error.InvalidXVVersion()), jsonSerializerSettings);
                }

                if (ex is UnsupportedVersionException exception)
                {
                    statusCode = (int)HttpStatusCode.NotAcceptable;
                    handledError = JsonConvert.SerializeObject(new ResponseErrorList(Error.UnsupportedXVVersion(exception.MinVersion, exception.MaxVersion)), jsonSerializerSettings);
                }

                if (!string.IsNullOrEmpty(handledError))
                {
                    context.Response.StatusCode = statusCode;
                    context.Response.ContentType = "application/json";
                    await context.Response.WriteAsync(handledError).ConfigureAwait(false);
                }
            }
        }
    }
}

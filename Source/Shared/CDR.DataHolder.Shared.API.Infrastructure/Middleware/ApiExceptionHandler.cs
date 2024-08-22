﻿using CDR.DataHolder.Shared.API.Infrastructure.Models;
using CDR.DataHolder.Shared.API.Infrastructure.Versioning;
using CDR.DataHolder.Shared.Domain.Models;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using System.Net;
using System.Threading.Tasks;

namespace CDR.DataHolder.Shared.API.Infrastructure.Middleware
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
                var jsonSerializerSettings = new CdrJsonSerializerSettings(); //TODO: This should be set as default through startup and we can remove this line

                if (ex is InvalidVersionException)
                {
                    statusCode = (int)HttpStatusCode.BadRequest;
                    handledError = JsonConvert.SerializeObject(new ResponseErrorList().AddInvalidXVInvalidVersion(), jsonSerializerSettings);
                }

                if (ex is UnsupportedVersionException)
                {
                    statusCode = (int)HttpStatusCode.NotAcceptable;
                    handledError = JsonConvert.SerializeObject(new ResponseErrorList().AddInvalidXVUnsupportedVersion(), jsonSerializerSettings);
                }

                if (ex is MissingRequiredHeaderException)
                {
                    statusCode = (int)HttpStatusCode.BadRequest;
                    handledError = JsonConvert.SerializeObject(new ResponseErrorList().AddInvalidXVMissingRequiredHeader(), jsonSerializerSettings);
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

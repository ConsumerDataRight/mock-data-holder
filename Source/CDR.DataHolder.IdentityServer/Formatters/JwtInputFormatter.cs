using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using CDR.DataHolder.IdentityServer.Models;
using Microsoft.AspNetCore.Mvc.Formatters;
using Microsoft.Extensions.Logging;
using Microsoft.Net.Http.Headers;

namespace CDR.DataHolder.IdentityServer.Formatters
{
    public class JwtInputFormatter : TextInputFormatter
    {
        public JwtInputFormatter()
        {
            SupportedMediaTypes.Add(MediaTypeHeaderValue.Parse("application/jwt"));
            SupportedEncodings.Add(UTF8EncodingWithoutBOM);
        }

        public override bool CanRead(InputFormatterContext context)
        {
            return context.HttpContext.Request.ContentType == "application/jwt";
        }

        public override async Task<InputFormatterResult> ReadRequestBodyAsync(InputFormatterContext context, Encoding encoding)
        {
            var serviceProvider = context.HttpContext.RequestServices;
            var logger = serviceProvider.GetService(typeof(ILogger<JwtInputFormatter>)) as ILogger;

            using var streamReader = new StreamReader(context.HttpContext.Request.Body);
            var body = await streamReader.ReadToEndAsync();

            ClientRegistrationRequest clientRegistrationRequest;
            try
            {
                clientRegistrationRequest = new ClientRegistrationRequest(body);

                // These two properties from JwtSecurityToken for ClientRegistrationRequest and SoftwareStatement
                // cause an error if a claim value is invalid for exp or nbf when the ReadRequestBodyAsync method returns.
                // Checking them here so can handle exception, log it, and return appropriate error.
                // If the claims are missing these calls use a default datetime min and are fine.
                // Further validation of request occurs in ClientRegistrationRequestValidator.
                _ = clientRegistrationRequest.ValidFrom;
                _ = clientRegistrationRequest.ValidTo;

                try
                {
                    var softwareStatement = new SoftwareStatement(clientRegistrationRequest.SoftwareStatementJwt);

                    _ = softwareStatement.ValidFrom;
                    _ = softwareStatement.ValidTo;

                    clientRegistrationRequest.SoftwareStatement = softwareStatement;
                }
                catch (Exception ex)
                {
                    // Cant log actual Exception as Azure AppService Logging crashes in some instances
                    logger.LogError("Error processing the SSA JWT {ExceptionMessage} {StackTrace}", ex.Message, ex.StackTrace);
                    context.ModelState.AddModelError(CdsConstants.RegistrationRequest.SoftwareStatement, "SSA is an invalid JWT");
                    return await InputFormatterResult.FailureAsync();
                }
            }
            catch (Exception ex)
            {
                // Cant log actual Exception as Azure AppService Logging crashes in some instances
                logger.LogError("Error processing the Request JWT {ExceptionMessage} {StackTrace}", ex.Message, ex.StackTrace);
                return await InputFormatterResult.FailureAsync();
            }

            return await InputFormatterResult.SuccessAsync(clientRegistrationRequest);
        }
    }
}

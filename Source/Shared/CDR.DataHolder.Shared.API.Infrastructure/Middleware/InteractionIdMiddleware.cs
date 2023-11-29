using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;

namespace CDR.DataHolder.Shared.Resource.API.Business.Middleware
{
	public class InteractionIdMiddleware
	{
		private const string HEADER_NAME = "x-fapi-interaction-id";
		private readonly RequestDelegate _next;
		

		public InteractionIdMiddleware(RequestDelegate next)
		{
			_next = next ?? throw new ArgumentNullException(nameof(next));
		}

		public Task Invoke(HttpContext context)
		{
			string interactionId = Guid.NewGuid().ToString();
			if (context.Request.Headers.TryGetValue(HEADER_NAME, out StringValues existingInteractionId))
			{
				interactionId = existingInteractionId;
			}

			// Apply the interaction ID to the response header for client side tracking
			context.Response.OnStarting(() =>
			{
				context.Response.Headers.Add(HEADER_NAME, new[] { interactionId });
				return Task.CompletedTask;
			});

			return _next(context);
		}
	}
}

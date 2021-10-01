using System;
using System.Linq;
using CDR.DataHolder.API.Infrastructure.Extensions;
using CDR.DataHolder.Resource.API.Business.Middleware;
using CDR.DataHolder.Resource.API.Business.Models;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Primitives;

namespace CDR.DataHolder.Resource.API.Business
{
    public static class Extensions
	{
		public static Links GetLinks(this ControllerBase controller, string routeName, int? currentPage = null, int totalPages = 0, int? pageSize = null)
		{
			string forwardedHost = null;
			if (controller.Request.Headers.TryGetValue("X-Forwarded-Host", out StringValues forwardedHosts))
			{
				forwardedHost = forwardedHosts.First();
			}

			var selfLink = ReplaceUriHost(controller.Request.GetDisplayUrl(), forwardedHost);

			// Construct the non-paginated links.
			if (currentPage == null || totalPages == 0)
			{
				return new Links()
				{
					Self = selfLink
				};
			}

            // Construct the paginated links
            var links = new LinksPaginated
            {
                Self = selfLink,
                First = controller.GetPageUri(selfLink, 1, pageSize),
                Last = controller.GetPageUri(selfLink, totalPages, pageSize)
            };

            if (currentPage <= 1)
			{
				links.Prev = null;
			}
			else
			{
				links.Prev = controller.GetPageUri(selfLink, currentPage - 1, pageSize);
			}

			if (currentPage >= totalPages)
			{
				links.Next = null;
			}
			else
			{
				links.Next = controller.GetPageUri(selfLink, currentPage + 1, pageSize);
			}

			return links;
		}

		public static Uri GetPageUri(this ControllerBase controller, Uri selfLink, int? page, int? pageSize)
		{
			if (!page.HasValue && !pageSize.HasValue)
			{
				return selfLink;
            }

			var query = selfLink.QueryToNameValueCollection();

			if (page.HasValue)
            {
				query.AddOrUpdate("page", page.Value.ToString());
            }

			if (pageSize.HasValue)
            {
				query.AddOrUpdate("page-size", pageSize.Value.ToString());
			}

			return new Uri(string.Format("{0}?{1}", selfLink.ToString().Split('?')[0], query.ToQueryString()));
		}

		private static Uri ReplaceUriHost(string url, string newHost = null)
		{
			var uriBuilder = new UriBuilder(url);
			if (!string.IsNullOrEmpty(newHost))
			{
				var segments = newHost.Split(':');
				uriBuilder.Host = segments[0];

				if (segments.Length > 1)
				{
					uriBuilder.Port = int.Parse(segments[1]);
				}
			}

			return uriBuilder.Uri;
		}

		public static Guid? GetSoftwareProductId(this ControllerBase controller)
		{
			string softwareProductIdString = controller.User.FindFirst("software_id")?.Value;
            Guid softwareProductId;
            if (Guid.TryParse(softwareProductIdString, out softwareProductId))
			{
				return softwareProductId;
			}
			return null;
		}

		public static IApplicationBuilder UseInteractionId(this IApplicationBuilder app)
		{
			if (app == null)
			{
				throw new ArgumentNullException(nameof(app));
			}

			return app.UseMiddleware<InteractionIdMiddleware>();
		}
	}
}

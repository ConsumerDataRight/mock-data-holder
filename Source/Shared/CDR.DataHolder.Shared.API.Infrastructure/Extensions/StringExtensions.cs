using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.Encodings.Web;

namespace CDR.DataHolder.Shared.API.Infrastructure.Extensions
{
    public static class StringExtensions
    {
        public static bool IsMissing(this string? value) => string.IsNullOrWhiteSpace(value);

        public static bool IsPresent(this string? value) => !string.IsNullOrWhiteSpace(value);

        public static string ToSpaceSeparatedString(this IEnumerable<string>? list)
        {
            if (list == null)
            {
                return string.Empty;
            }

            var sb = new StringBuilder(1000);

            foreach (var element in list)
            {
                sb.Append(element + " ");
            }

            return sb.ToString().Trim();
        }

        public static bool IsMissingOrTooLong(this string? value, int maxLength)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return true;
            }

            if (value.Length > maxLength)
            {
                return true;
            }

            return false;
        }

        public static string? EnsureTrailingSlash(this string? url)
        {
            if (url != null && !url.EndsWith('/'))
            {
                return url + "/";
            }

            return url;
        }

        /// <summary>
        /// Converts the specified string to title case (except for words that are entirely in uppercase, which are considered to be acronyms).
        /// </summary>
        /// <param name="value">The string to convert to title case.</param>
        /// <returns>The specified string converted to title case.</returns>
        public static string ToTitleCase(this string value)
            => value.IsPresent() ? CultureInfo.CurrentCulture.TextInfo.ToTitleCase(value) : value;

        public static bool IsAnyOf(this string value, params string[] options)
            => Array.Find(options, x => x == value) != null;

        public static string AddQueryString(this string? url, string? query)
        {
            if (url == null)
            {
                throw new ArgumentNullException(nameof(url));
            }

            if (query == null)
            {
                throw new ArgumentNullException(nameof(query));
            }

            if (!url.Contains('?'))
            {
                url += "?";
            }
            else if (!url.EndsWith('&'))
            {
                url += "&";
            }

            return url + query;
        }

        public static string AddQueryString(this string? url, string? name, string? value)
        {
            if (url == null)
            {
                throw new ArgumentNullException(nameof(url));
            }

            if (string.IsNullOrWhiteSpace(name))
            {
                throw new ArgumentException("{0} was not provided", nameof(name));
            }

            return url.AddQueryString($"{name}={(value == null ? string.Empty : UrlEncoder.Default.Encode(value))}");
        }

        public static string AddHashFragment(this string? url, string? query)
        {
            if (url == null)
            {
                throw new ArgumentNullException(nameof(url));
            }

            if (query == null)
            {
                throw new ArgumentNullException(nameof(query));
            }

            if (!url.Contains('#'))
            {
                url += "#";
            }

            return url + query;
        }

        public static IEnumerable<string> FromSpaceSeparatedString(this string? input)
        {
            if (input == null)
            {
                return Array.Empty<string>();
            }

            input = input.Trim();
            return input.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries).ToList();
        }

        public static List<string> ParseScopesString(this string? scopes)
        {
            if (scopes.IsMissing())
            {
                return new List<string>();
            }

            scopes = scopes?.Trim();
            var parsedScopes = scopes?.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries).Distinct().ToList();

            if (parsedScopes != null && parsedScopes.Any())
            {
                parsedScopes.Sort();
                return parsedScopes;
            }

            return new List<string>();
        }
    }
}

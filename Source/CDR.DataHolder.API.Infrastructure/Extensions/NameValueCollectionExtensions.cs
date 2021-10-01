using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Text;
using System.Text.Encodings.Web;

namespace CDR.DataHolder.API.Infrastructure.Extensions
{
    public static class NameValueCollectionExtensions
    {
        public static string ToQueryString(this NameValueCollection collection)
        {
            if (collection.Count == 0)
            {
                return String.Empty;
            }

            var builder = new StringBuilder(128);
            var first = true;
            foreach (string name in collection)
            {
                var values = collection.GetValues(name);
                if (values == null || values.Length == 0)
                {
                    first = AppendNameValuePair(builder, first, true, name, String.Empty);
                }
                else
                {
                    foreach (var value in values)
                    {
                        first = AppendNameValuePair(builder, first, true, name, value);
                    }
                }
            }

            return builder.ToString();
        }

        public static NameValueCollection QueryToNameValueCollection(this Uri uri)
        {
            var queryCollection = new NameValueCollection();
            var queryString = uri.Query.TrimStart('?');

            if (string.IsNullOrEmpty(queryString))
            {
                return queryCollection;
            }

            foreach (var parameter in queryString.Split('&'))
            {
                var nameValue = parameter.Split('=');
                queryCollection.Add(nameValue[0], nameValue.Length > 1 ? nameValue[1] : string.Empty);
            }

            return queryCollection;
        }

        public static void AddOrUpdate(this NameValueCollection collection, string key, string value)
        {
            if (collection.AllKeys.Any(k => k == key))
            {
                collection[key] = value;
            }
            else
            {
                collection.Add(key, value);
            }
        }

        public static Dictionary<string, string> ToScrubbedDictionary(this NameValueCollection collection, params string[] nameFilter)
        {
            var dict = new Dictionary<string, string>();

            if (collection == null || collection.Count == 0)
            {
                return dict;
            }

            foreach (string name in collection)
            {
                var value = collection.Get(name);
                if (value != null)
                {
                    if (nameFilter.Contains(name))
                    {
                        value = "***REDACTED***";
                    }

                    dict.Add(name, value);
                }
            }

            return dict;
        }


        internal static string ConvertFormUrlEncodedSpacesToUrlEncodedSpaces(string str)
        {
            if ((str != null) && (str.IndexOf('+') >= 0))
            {
                str = str.Replace("+", "%20");
            }
            return str;
        }


        private static bool AppendNameValuePair(StringBuilder builder, bool first, bool urlEncode, string name, string value)
        {
            var effectiveName = name ?? String.Empty;
            var encodedName = urlEncode ? UrlEncoder.Default.Encode(effectiveName) : effectiveName;

            var effectiveValue = value ?? String.Empty;
            var encodedValue = urlEncode ? UrlEncoder.Default.Encode(effectiveValue) : effectiveValue;
            encodedValue = ConvertFormUrlEncodedSpacesToUrlEncodedSpaces(encodedValue);

            if (first)
            {
                first = false;
            }
            else
            {
                builder.Append("&");
            }

            builder.Append(encodedName);
            builder.Append("=");
            if (!String.IsNullOrEmpty(encodedValue))
            {
                builder.Append(encodedValue);
            }
            return first;
        }
    }
}
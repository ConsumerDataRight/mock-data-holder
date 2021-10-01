using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using Microsoft.Extensions.Primitives;
using static CDR.DataHolder.IdentityServer.CdsConstants;

namespace CDR.DataHolder.IdentityServer.Extensions
{
    public static class CollectionExtensions
    {
        public static NameValueCollection AsNameValueCollection(this IDictionary<string, StringValues> collection)
        {
            var values = new NameValueCollection();
            foreach (KeyValuePair<string, StringValues> pair in collection)
            {
                string introduced3 = pair.Key;
                values.Add(introduced3, Enumerable.First<string>(pair.Value));
            }
            return values;
        }

        public static NameValueCollection AsNameValueCollection(this IEnumerable<KeyValuePair<string, StringValues>> collection)
        {
            var values = new NameValueCollection();
            foreach (KeyValuePair<string, StringValues> pair in collection)
            {
                string introduced3 = pair.Key;
                values.Add(introduced3, Enumerable.First<string>(pair.Value));
            }
            return values;
        }

        public static bool AreScopesValid(this IEnumerable<string> scopes, bool ignoreIdentityScopes = false)
        {
            return true;
        }

        public static bool ContainsOpenIdScopes(this IEnumerable<string> scopes)
        {
            return scopes.ContainsScope(StandardScopes.OpenId);
        }

        public static bool ContainsOfflineScopes(this IEnumerable<string> scopes)
        {
            return scopes.ContainsScope(StandardScopes.OfflineAccess);
        }

        public static bool ContainsApiResourceScopes(this IEnumerable<string> scopes)
        {
            return scopes.ContainsScope(CdsConstants.ApiScopes.Banking.Accounts) || scopes.ContainsScope(CdsConstants.ApiScopes.Banking.Transactions);
        }

        public static bool ContainsScope(this IEnumerable<string> scopes, string scope)
        {
            return scopes.Contains(scope);
        }

        public static bool AreScopesAllowed(this IEnumerable<string> requestedScopes, IEnumerable<string> allowedScopes)
        {
            return !requestedScopes.Except(allowedScopes).Any();
        }
        
        public static bool IsResponseTypeValid(this IEnumerable<string> scopes, string responseType)
        {
            return true;
        }

    }
}

using System;
using System.Collections.Specialized;
using CDR.DataHolder.API.Infrastructure.Extensions;
using IdentityServer4.ResponseHandling;

namespace CDR.DataHolder.IdentityServer.Extensions
{
    public static class AuthorizeResponseExtensions
    {
        public static NameValueCollection ToNameValueCollection(this AuthorizeResponse response)
        {
            var collection = new NameValueCollection();

            Add(collection, "state", response.State);
            Add(collection, "session_state", response.SessionState);

            if (response.IsError)
            {
                Add(collection, "error", response.Error);
                Add(collection, "error_description", response.ErrorDescription);
                return collection;
            }

            Add(collection, "code", response.Code);
            Add(collection, "id_token", response.IdentityToken);
            Add(collection, "scope", response.Scope);

            if (response.AccessToken.IsPresent())
            {
                collection.Add("access_token", response.AccessToken);
                collection.Add("token_type", "Bearer");
                collection.Add("expires_in", response.AccessTokenLifetime.ToString());
            }

            return collection;
        }

        private static void Add(NameValueCollection collection, string name, string value)
        {
            if (value.IsPresent())
            {
                collection.Add(name, value);
            }
        }
    }
}

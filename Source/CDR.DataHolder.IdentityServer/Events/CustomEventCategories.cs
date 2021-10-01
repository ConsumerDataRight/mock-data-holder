using System.Collections.Generic;
using IdentityServer4.Events;

namespace CDR.DataHolder.IdentityServer.Events
{
    public static class CustomEventCategories
    {
        public const string Claims = "Claims";

        public const string MTLS = "MTLS";

        public const string ClientAssertion = "ClientAssertion";

        public const string Request = "Request";

        public const string TokenRequest = "TokenRequest";

        public const string Authentication = "Authentication";

        public const string Response = "Response";
    }
}
using System;
using System.Net;

namespace CDR.DataHolder.IntegrationTests.Models
{
    public class AuthoriseException : Exception
    {
        public HttpStatusCode StatusCode { get; }

        public string Error { get; }

        public string ErrorDescription { get; }

        public AuthoriseException() { }

        public AuthoriseException(string message)
            : base(message) { }

        public AuthoriseException(string message, Exception inner)
            : base(message, inner) { }

        public AuthoriseException(string message, HttpStatusCode statusCode, string error, string errorDescription)
            : this(message)
        {
            StatusCode = statusCode;
            Error = error;
            ErrorDescription = errorDescription;
        }
    }
}

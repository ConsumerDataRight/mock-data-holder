using System;

namespace CDR.DataHolder.Shared.API.Infrastructure.Versioning
{
    public class InvalidVersionException : Exception
    {
        public string? HeaderName { get; set; }

        public InvalidVersionException()
            : base()
        {
        }

        public InvalidVersionException(string headerName)
            : base()
        {
            this.HeaderName = headerName;
        }
    }
}

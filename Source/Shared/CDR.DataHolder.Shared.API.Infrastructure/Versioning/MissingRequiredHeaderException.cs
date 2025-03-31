using System;

namespace CDR.DataHolder.Shared.API.Infrastructure.Versioning
{
    public class MissingRequiredHeaderException : Exception
    {
        public string? HeaderName { get; set; }

        public MissingRequiredHeaderException()
            : base()
        {
        }

        public MissingRequiredHeaderException(string headerName)
            : base()
        {
            this.HeaderName = headerName;
        }
    }
}

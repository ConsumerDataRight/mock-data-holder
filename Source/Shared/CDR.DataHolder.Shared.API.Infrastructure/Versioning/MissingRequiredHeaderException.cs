using System;
using System.Runtime.Serialization;

namespace CDR.DataHolder.Shared.API.Infrastructure.Versioning
{
    [Serializable]
    public class MissingRequiredHeaderException : Exception
    {
        public string? HeaderName { get; set; }

        public MissingRequiredHeaderException() : base() { }

        public MissingRequiredHeaderException(string headerName) : base()
        {
            this.HeaderName = headerName;
        }

        protected MissingRequiredHeaderException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }

        public override void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            base.GetObjectData(info, context);
        }
    }
}

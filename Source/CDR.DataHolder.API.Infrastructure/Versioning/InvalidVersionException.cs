using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace CDR.DataHolder.API.Infrastructure.Versioning
{
    [Serializable]
    public class InvalidVersionException : Exception
    {
        public string HeaderName { get; set; }

        public InvalidVersionException() : base() { }

        public InvalidVersionException(string headerName) : base()
        {
            this.HeaderName = headerName;
        }

        protected InvalidVersionException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }

        public override void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            base.GetObjectData(info, context);
        }

    }
}

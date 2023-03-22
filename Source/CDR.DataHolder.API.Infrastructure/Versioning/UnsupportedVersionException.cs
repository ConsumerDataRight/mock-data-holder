using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace CDR.DataHolder.API.Infrastructure.Versioning
{
    [Serializable]
    public class UnsupportedVersionException : Exception
    {
        public int MinVersion { get; set; }

        public int MaxVersion { get; set; }

        public UnsupportedVersionException() : base() 
        {
            this.MinVersion = 1;
            this.MaxVersion = 1;
        }

        public UnsupportedVersionException(string message) : base(message) 
        {
            this.MinVersion = 1;
            this.MaxVersion = 1;
        }

        public UnsupportedVersionException(int minVersion, int maxVersion)
            : base($"minimum version: {minVersion}, maximum version: {maxVersion}")
        {
            this.MinVersion = minVersion;
            this.MaxVersion = maxVersion;
        }

        protected UnsupportedVersionException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }

        public override void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            base.GetObjectData(info, context);
        }
    }
}

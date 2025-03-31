using System;

namespace CDR.DataHolder.Shared.API.Infrastructure.Versioning
{
    public class UnsupportedVersionException : Exception
    {
        public int MinVersion { get; set; }

        public int MaxVersion { get; set; }

        public UnsupportedVersionException()
            : base()
        {
            this.MinVersion = 1;
            this.MaxVersion = 1;
        }

        public UnsupportedVersionException(string message)
            : base(message)
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
    }
}

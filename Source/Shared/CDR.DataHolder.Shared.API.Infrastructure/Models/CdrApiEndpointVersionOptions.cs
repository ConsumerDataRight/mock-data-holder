namespace CDR.DataHolder.Shared.API.Infrastructure.Models
{
    public class CdrApiEndpointVersionOptions
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="CdrApiEndpointVersionOptions"/> class.
        /// Constructs an option set where multiple versions of the endpoint are supported.
        /// </summary>
        public CdrApiEndpointVersionOptions(string path, bool isXvMandatory, int minVersion, int maxVersion)
        {
            this.Path = path;
            this.IsVersioned = true;
            this.IsXVHeaderMandatory = isXvMandatory;
            this.CurrentMinVersion = minVersion;
            this.CurrentMaxVersion = maxVersion;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="CdrApiEndpointVersionOptions"/> class.
        /// Constructs an option set where only one version of the endpoint is supported.
        /// </summary>
        public CdrApiEndpointVersionOptions(string path, bool isXvMandatory, int version)
        {
            this.Path = path;
            this.IsVersioned = true;
            this.IsXVHeaderMandatory = isXvMandatory;
            this.CurrentMinVersion = version;
            this.CurrentMaxVersion = version;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="CdrApiEndpointVersionOptions"/> class.
        /// Constructs an option set where the endpoint is unversioned.
        /// </summary>
        public CdrApiEndpointVersionOptions(string path)
        {
            this.Path = path;
            this.IsVersioned = false;
            this.IsXVHeaderMandatory = false;
        }

        public string Path { get; }

        public bool IsVersioned { get; }

        public bool IsXVHeaderMandatory { get; }

        public int? CurrentMinVersion { get; }

        public int? CurrentMaxVersion { get; }
    }
}

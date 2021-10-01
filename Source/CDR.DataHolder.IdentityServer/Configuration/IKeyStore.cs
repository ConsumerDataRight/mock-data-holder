using System;

namespace CDR.DataHolder.IdentityServer.Configuration
{
    public interface IKeyStore
    {
        public Uri BaseUri { get; set; }

        string SigningKeyRegisterRsa { get; set; }

        string SigningKeyRsa { get; set; }

        string SigningKeyEcdsa { get; set; }

        int SigningKeyRolloverMinutes { get; set; }
    }
}
using System;

namespace CDR.DataHolder.IdentityServer.Configuration
{
    public class KeyStore : IKeyStore
    {
        public Uri BaseUri { get; set; }

        public string SigningKeyRegisterRsa { get; set; }

        public string SigningKeyRsa { get; set; }

        public string SigningKeyEcdsa { get; set; }

        public int SigningKeyRolloverMinutes { get; set; }
    }
}
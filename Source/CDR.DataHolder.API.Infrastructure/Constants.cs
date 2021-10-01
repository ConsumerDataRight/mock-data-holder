namespace CDR.DataHolder.API.Infrastructure
{
    public static class Constants
    {
        public static class CustomHeaders
        {
            public const string ClientCertThumbprintHeaderKey = "X-TlsClientCertThumbprint";
            public const string ClientCertClientNameHeaderKey = "X-TlsClientCertCN";
            public const string ApiVersionHeaderKey = "x-v";
        }
    }
}

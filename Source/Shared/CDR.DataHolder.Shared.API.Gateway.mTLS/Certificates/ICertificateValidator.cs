﻿using System.Security.Cryptography.X509Certificates;

namespace CDR.DataHolder.Shared.API.Gateway.Mtls.Certificates
{
    public interface ICertificateValidator
    {
        void ValidateClientCertificate(X509Certificate2 clientCert);
    }
}

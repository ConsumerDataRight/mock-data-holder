using System.Security.Cryptography.X509Certificates;

namespace CDR.DataHolder.API.Gateway.mTLS.Certificates
{
    public interface ICertificateValidator
    {
        void ValidateClientCertificate(X509Certificate2 clientCert);
    }
}

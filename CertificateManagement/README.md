# Certificate Management

Certificates play an important part in the CDR ecosystem to establish trust between participants and protect communications.  **DigiCert** is the Certificate Authority (CA) for the CDR and the ACCC is responsible for provisioning DigiCert certificates to participants during the on-boarding process.

For more information, consult the [Certificate Management](https://cdr-register.github.io/register/#certificate-management) section of the Register Design.

The Mock Data Holder will mimic the behaviour of a data holder in the CDR ecosystem and therefore will use certificates in its interactions.  However, the use of DigiCert for this purpose is not feasible or scalable so an alternative approach is adopted.

There are 2 areas where certificates are used within the Mock Data Holder:
- mTLS
- TLS

## mTLS

**Mutual Transport Layer Security** is used extensively within the CDR ecosystem.  Data Recipients are provisioned client certificates and will present that certificate when interacting with a Data Holder for consumer data sharing and with the Register when discovering Data Holder Brands and request an SSA.  Data Holders are issued a server certificate for their side of the interaction.  All participants need to validate the certificates presented during the establishment of a mTLS session.

The server certificate used by the Mock Data Holder for mTLS connections is available at `mtls\server.pfx` and has the password of `#M0ckDataHolder#`.

### Certificate Authority

A self-signed Root CA has been provisioned to handle certificate provisioning and to be used in the certificate validation processes.  The server certificate/s for the mock data holder is generated from the self-signed Root CA.  

Consult the [CertificateManagement](https://github.com/ConsumerDataRight/mock-register/tree/main/CertificateManagement) section of the [Mock Register](https://github.com/ConsumerDataRight/mock-register/) for more information.

## TLS

Endpoints that are not protected by mTLS are protected by TLS.  The server certificate used for TLS communication can be provisioned by the CDR CA, or alternatively participants can used a trusted third party CA.

For the Mock Data Holder, a self-signed TLS certificate is used.  The self-signed certificate will be issued by the Mock CDR CA, like the mTLS certificates are.

The TLS certificate for the Mock Data Holder is available at `tls\mock-data-holder.pfx` and has the password of `#M0ckDataHolder#`.

using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Text.Json.Serialization;

namespace CDR.DataHolder.IdentityServer.Models
{
    public class SoftwareStatement : JwtSecurityToken
    {
        public SoftwareStatement(string jwtEncodedString)
            : base(jwtEncodedString)
        {
        }

        /// <summary>
        /// Gets a unique identifier assigned by the CDR Register that identifies the Accredited Data Recipient Legal Entity.
        /// </summary>
        [JsonPropertyName("legal_entity_id")]
        public string LegalEntityId => Claims.FirstOrDefault(x => x.Type == "legal_entity_id")?.Value;

        /// <summary>
        /// Gets a Human-readable string name of the Accredited Data Recipient Legal Entity.
        /// </summary>
        [JsonPropertyName("legal_entity_name")]
        public string LegalEntityName => Claims.FirstOrDefault(x => x.Type == "legal_entity_name")?.Value;

        /// <summary>
        /// Gets a unique identifier string assigned by the CDR Register that identifies the Accredited Data Recipient Brand.
        /// </summary>
        [JsonPropertyName("org_id")]
        public string OrgId => Claims.FirstOrDefault(x => x.Type == "org_id")?.Value;

        /// <summary>
        /// Gets a Human-readable string name of the Accredited Data Recipient to be presented to the end user during authorization.
        /// </summary>
        [JsonPropertyName("org_name")]
        public string OrgName => Claims.FirstOrDefault(x => x.Type == "org_name")?.Value;

        /// <summary>
        /// Gets a Human-readable string name of the software product to be presented to the end-user during authorization.
        /// </summary>
        [JsonPropertyName("client_name")]
        public string ClientName => Claims.FirstOrDefault(x => x.Type == "client_name")?.Value;

        /// <summary>
        /// Gets a Human-readable string name of the software product description to be presented to the end user during authorization.
        /// </summary>
        [JsonPropertyName("client_description")]
        public string ClientDescription => Claims.FirstOrDefault(x => x.Type == "client_description")?.Value;

        /// <summary>
        /// Gets a URL string of a web page providing information about the client.
        /// </summary>
        [JsonPropertyName("client_uri")]
        public string ClientUri => Claims.FirstOrDefault(x => x.Type == "client_uri")?.Value;

        /// <summary>
        /// Gets a URL string that references a logo for the client. If present, the server SHOULD display this image to the end-user during approval
        /// </summary>
        [JsonPropertyName("logo_uri")]
        public string LogoUri => Claims.FirstOrDefault(x => x.Type == "logo_uri")?.Value;

        /// <summary>
        /// URL string that points to a human-readable terms of service document for the Software Product.
        /// </summary>
        /// <value>URL string that points to a human-readable terms of service document for the Software Product</value>
        [JsonPropertyName("tos_uri")]
        public string TosUri => Claims.FirstOrDefault(x => x.Type == "tos_uri")?.Value;

        /// <summary>
        /// Gets a URL string that points to a human-readable policy document for the Software Product.
        /// </summary>
        [JsonPropertyName("policy_uri")]
        public string PolicyUri => Claims.FirstOrDefault(x => x.Type == "policy_uri")?.Value;

        /// <summary>
        /// Gets a URL string referencing the client JSON Web Key (JWK) Set [RFC7517] document, which contains the client public keys.
        /// </summary>
        [JsonPropertyName("jwks_uri")]
        public string JwksUri => Claims.FirstOrDefault(x => x.Type == "jwks_uri")?.Value;

        /// <summary>
        /// Gets a URI string that references the location of the Software Product consent revocation endpoint.
        /// </summary>
        [JsonPropertyName("revocation_uri")]
        public string RevocationUri => Claims.FirstOrDefault(x => x.Type == "revocation_uri")?.Value;

        /// <summary>
        /// Gets a URI string that references the location of the Software Product recipient base uri endpoint.
        /// </summary>
        [JsonPropertyName("recipient_base_uri")]
        public string RecipientBaseUri => Claims.FirstOrDefault(x => x.Type == "recipient_base_uri")?.Value;

        /// <summary>
        /// Gets a String representing a unique identifier assigned by the ACCC Register and used by registration endpoints to identify the software product to be dynamically registered. &lt;/br&gt;&lt;/br&gt;The \&quot;software_id\&quot; will remain the same for the lifetime of the product, across multiple updates and versions
        /// </summary>
        [JsonPropertyName("software_id")]
        public string SoftwareId => Claims.FirstOrDefault(x => x.Type == "software_id")?.Value;

        /// <summary>
        /// Gets a String containing a space-separated list of scope values that the client can use when requesting access tokens.
        /// </summary>
        [JsonPropertyName("scope")]
        public string Scope => Claims.FirstOrDefault(x => x.Type == "scope")?.Value;

        /// <summary>
        /// Gets an Array of redirection URI strings for use in redirect-based flows.
        /// </summary>
        [JsonPropertyName("redirect_uris")]
        public IEnumerable<string> RedirectUris => Claims.Where(x => x.Type == "redirect_uris").Select(x => x.Value).ToArray();

        /// <summary>
        ///Sector Identifier Uri used in PPID calculations.
        /// </summary>
        [JsonPropertyName("sector_identifier_uri")]
        public string SectorIdentifierUri => Claims.FirstOrDefault(x => x.Type == "sector_identifier_uri")?.Value;
    }
}
using System.ComponentModel.DataAnnotations;

namespace CDR.DataHolder.IdentityServer.UnitTests
{
    public class ClientAssertionJwt
    {
        [Required(AllowEmptyStrings = false)]
        public string Iss { get; set; }

        [Required]
        public string Sub { get; set; }

        [Required]
        public long Iat { get; set; }

        [Required]
        public long? Exp { get; set; }

        [Required(AllowEmptyStrings = false)]
        public string Jti { get; set; }

        public string Aud { get; set; }
    }
}

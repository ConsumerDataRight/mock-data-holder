namespace CDR.DataHolder.Shared.Domain.Entities
{
    public class UserInfoClaims
    {
        public string GivenName { get; set; } = string.Empty;

        public string FamilyName { get; set; } = string.Empty;

        public string Name { get; set; } = string.Empty;

        public string Audience { get; set; } = string.Empty;

        public DateTime? LastUpdated { get; set; }
    }
}

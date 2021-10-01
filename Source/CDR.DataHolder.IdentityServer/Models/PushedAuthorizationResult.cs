namespace CDR.DataHolder.IdentityServer.Models
{
    public class PushedAuthorizationResult
    {
        public bool HasError
        {
            get
            {
                return !string.IsNullOrWhiteSpace(Error);
            }
        }

        public string Error { get; set; }

        public string ErrorDescription { get; set; }

        public string RequestUri { get; set; }
    }
}

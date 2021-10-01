using System.Text.Json.Serialization;

namespace CDR.DataHolder.IdentityServer.Models
{
    /// <summary>
    /// Model used to serialzie any unhandled exception caught by exception middleware.
    /// Contains friendly error message for the client.
    /// </summary>
    public class ApiError
    {
        [JsonPropertyName("message")]
        public string Message { get; set; }
    }
}
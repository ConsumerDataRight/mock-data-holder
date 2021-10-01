using IdentityServer4.Stores.Serialization;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;

namespace CDR.DataHolder.IdentityServer.Extensions
{
    public static class SerializationExtensions
    {
        public static string ToJson(this object value)
        {
            var jsonSerializerSettings = new JsonSerializerSettings
            {
                ContractResolver = new CamelCasePropertyNamesContractResolver(),
                DefaultValueHandling = DefaultValueHandling.Include,
                NullValueHandling = NullValueHandling.Ignore,
                Formatting = Formatting.Indented,
            };
            jsonSerializerSettings.Converters.Add(new ClaimConverter());
            jsonSerializerSettings.Converters.Add(new ClaimsPrincipalConverter());

            return JsonConvert.SerializeObject(value, jsonSerializerSettings);
        }
    }
}

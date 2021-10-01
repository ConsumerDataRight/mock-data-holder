using System;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace CDR.DataHolder.IdentityServer.Extensions
{
    public static class HttpClientExtensions
    {
        public static async Task<HttpResponseMessage> PostAsJsonAsync<T>(this HttpClient client, string requestUri, T content)
        {
            using var jsonContent = new StringContent(content.ToJson(), Encoding.UTF8, "application/json");
            return await client.PostAsync(new Uri(requestUri), jsonContent);
        }

        public static async Task<HttpResponseMessage> PostAsJsonAsync<T>(this HttpClient client, Uri requestUri, T content)
        {
            using var jsonContent = new StringContent(content.ToJson(), Encoding.UTF8, "application/json");
            return await client.PostAsync(requestUri, jsonContent);
        }

        public static async Task<T> ReadAsJson<T>(this HttpContent content)
            where T : class, new()
        {
            var contentAsString = await content.ReadAsStringAsync();
            return JsonConvert.DeserializeObject<T>(contentAsString);
        }
    }
}
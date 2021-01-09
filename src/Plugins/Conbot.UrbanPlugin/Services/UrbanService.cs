using System;
using System.Net.Http;
using System.Threading.Tasks;

using Newtonsoft.Json;

namespace Conbot.UrbanPlugin
{
    internal sealed class UrbanService : IDisposable
    {
        private const string ApiUrl = "http://api.urbandictionary.com/v0/define?term=";

        private readonly HttpClient _client;

        public UrbanService() => _client = new HttpClient();

        public async Task<UrbanSearchResult> SearchAsync(string query)
        {
            string? json = await _client.GetStringAsync($"{ApiUrl}{query}");
            return JsonConvert.DeserializeObject<UrbanSearchResult>(json);
        }

        public async Task<UrbanSearchResult> GetRandomAsync()
        {
            var response = await _client.GetAsync("https://www.urbandictionary.com/random.php?page=1");

            if (response?.RequestMessage?.RequestUri == null)
                return new UrbanSearchResult();

            return await SearchAsync(response.RequestMessage.RequestUri.AbsoluteUri
                .Replace("https://www.urbandictionary.com/define.php?term=", ""));
        }

        public void Dispose() => _client.Dispose();
    }
}

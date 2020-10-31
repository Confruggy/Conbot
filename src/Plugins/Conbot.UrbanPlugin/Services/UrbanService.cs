using System;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace Conbot.UrbanPlugin
{
    public class UrbanService : IDisposable
    {
        private static readonly string _apiUrl = "http://api.urbandictionary.com/v0/define?term=";

        private readonly HttpClient _client;

        public UrbanService() => _client = new HttpClient();

        public async Task<UrbanSearchResult> SearchAsync(string query)
        {
            var json = await _client.GetStringAsync($"{_apiUrl}{query}");
            var result = JsonConvert.DeserializeObject<UrbanSearchResult>(json);
            return result;
        }

        public async Task<UrbanSearchResult> GetRandomAsync()
        {
            var response = await _client.GetAsync("https://www.urbandictionary.com/random.php?page=1");
            return await SearchAsync(response.RequestMessage.RequestUri.AbsoluteUri
                .Replace("https://www.urbandictionary.com/define.php?term=", ""));
        }

        public void Dispose() => _client.Dispose();
    }
}
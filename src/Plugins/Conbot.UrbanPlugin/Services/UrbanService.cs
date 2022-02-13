using System;
using System.Net.Http;
using System.Threading.Tasks;

using Microsoft.Extensions.Configuration;

using Newtonsoft.Json;

namespace Conbot.UrbanPlugin;

internal sealed class UrbanService : IDisposable
{
    private readonly HttpClient _client;

    public string ApiBaseUrl { get; }
    public string WebsiteBaseUrl { get; }

    public UrbanService(IConfiguration configuration)
    {
        _client = new HttpClient();
        ApiBaseUrl = configuration.GetValue<string>("UrbanPlugin:UrbanDictionary:ApiBaseUrl");
        WebsiteBaseUrl = configuration.GetValue<string>("UrbanPlugin:UrbanDictionary:WebsiteBaseUrl");
    }

    public async Task<UrbanSearchResult> SearchAsync(string query)
    {
        string json = await _client.GetStringAsync($"{ApiBaseUrl}/define?term={query}");
        return JsonConvert.DeserializeObject<UrbanSearchResult>(json) ?? new UrbanSearchResult();
    }

    public async Task<UrbanSearchResult> GetRandomAsync()
    {
        var response = await _client.GetAsync($"{WebsiteBaseUrl}/random.php?page=1");

        if (response.RequestMessage?.RequestUri is null)
            return new UrbanSearchResult();

        return await SearchAsync(response.RequestMessage.RequestUri.AbsoluteUri
            .Replace($"{WebsiteBaseUrl}/define.php?term=", ""));
    }

    public void Dispose() => _client.Dispose();
}
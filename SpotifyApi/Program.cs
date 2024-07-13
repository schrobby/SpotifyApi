using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace SpotifyApi;

public class SpotifyAddToQueue
{
    private const string ClientId = "client_id";
    private const string ClientSecret = "client_secret";
    private const string RedirectUri = "http://localhost:8080/callback";
    private string accessToken = "";

    private readonly HttpClient httpClient = new();

    public async Task StartAuthenticationAsync()
    {
        var authUrl = "https://accounts.spotify.com/authorize";
        var parameters = new Dictionary<string, string>
        {
            { "client_id", ClientId },
            { "response_type", "code" },
            { "redirect_uri", RedirectUri },
            { "scope", "user-modify-playback-state" }
        };

        var queryString = new FormUrlEncodedContent(parameters).ReadAsStringAsync().Result;
        authUrl += "?" + queryString;

        Console.WriteLine($"URL: {authUrl}");
        Console.WriteLine("Enter code: ");
        var authCode = Console.ReadLine();

        if (authCode != null) 
            await ExchangeCodeForTokenAsync(authCode);
        else
        {
            Console.WriteLine("No auth code");
            Environment.Exit(1);
        }
    }

    private async Task ExchangeCodeForTokenAsync(string code)
    {
        var tokenUrl = "https://accounts.spotify.com/api/token";
        var content = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            { "grant_type", "authorization_code" },
            { "code", code },
            { "redirect_uri", RedirectUri }
        });

        var authHeader = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{ClientId}:{ClientSecret}"));
        httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", authHeader);

        var response = await httpClient.PostAsync(tokenUrl, content);
        var responseBody = await response.Content.ReadAsStringAsync();

        if (response.IsSuccessStatusCode)
        {
            var tokenResponse = JsonSerializer.Deserialize<TokenResponse>(responseBody);
            accessToken = tokenResponse.AccessToken;
            Console.WriteLine("Access token obtained");
            await AddSongToQueueAsync("spotify:track:09bdhPbvXd7B3TOa7ainYN"); 
        }
        else
            Console.WriteLine($"Failed to obtain access token: {responseBody}");
    }

    private async Task AddSongToQueueAsync(string trackUri)
    {
        var queueUrl = $"https://api.spotify.com/v1/me/player/queue?uri={Uri.EscapeDataString(trackUri)}";

        var request = new HttpRequestMessage(HttpMethod.Post, queueUrl);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        var response = await httpClient.SendAsync(request);
        var responseBody = await response.Content.ReadAsStringAsync();

        if (response.StatusCode == HttpStatusCode.NoContent || response.StatusCode == HttpStatusCode.OK)
            Console.WriteLine("Song added to queue successfully");
        else
        {
            Console.WriteLine($"Failed to add song to queue: {response.StatusCode} {response.ReasonPhrase}");
            Console.WriteLine(responseBody);
        }
    }

    private class TokenResponse
    {
        [JsonPropertyName("access_token")]
        public string AccessToken { get; set; }

        [JsonPropertyName("token_type")]
        public string TokenType { get; set; }

        [JsonPropertyName("expires_in")]
        public int ExpiresIn { get; set; }

        [JsonPropertyName("refresh_token")]
        public string RefreshToken { get; set; }

        [JsonPropertyName("scope")]
        public string Scope { get; set; }
    }
}

class Program
{
    private static async Task Main()
    {
        var spotifyIntegration = new SpotifyAddToQueue();
        await spotifyIntegration.StartAuthenticationAsync();
    }
}
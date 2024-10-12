using System.Net.Http.Headers;
using System.Text.Json;
using sportify.backend.Models;

public class SpotifyService
{
    private readonly HttpClient _httpClient;

    public SpotifyService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<UserInfo?> GetUserInfoAsync(string accessToken)
    {
        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        var response = await _httpClient.GetAsync("https://api.spotify.com/v1/me");

        if (!response.IsSuccessStatusCode) return null;

        var fetchedResponse = await response.Content.ReadAsStringAsync();
        // Get display_name and email from the response
        var userInfo = JsonDocument.Parse(fetchedResponse);
        var displayName = userInfo.RootElement.GetProperty("display_name").GetString();
        var email = userInfo.RootElement.GetProperty("email").GetString();

        return new UserInfo(displayName, email);
        
    }

    public async Task<List<Song>> GetRecentlyPlayedTracksAsync(string accessToken, int limit)
    {
        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        var response = await _httpClient.GetAsync($"https://api.spotify.com/v1/me/player/recently-played?limit={limit}");

        if (!response.IsSuccessStatusCode) return null;

        var recentlyPlayedContent = await response.Content.ReadAsStringAsync();

        Console.WriteLine(recentlyPlayedContent);

        var recentlyPlayedData = JsonDocument.Parse(recentlyPlayedContent);
        var items = recentlyPlayedData.RootElement.GetProperty("items");

        var tracks = new List<Song>();

        foreach (var item in items.EnumerateArray())
        {

            var track = item.GetProperty("track");
            var song = new Song
            {
                Id = track.GetProperty("id").GetString(),
                Title = track.GetProperty("name").GetString(),
                Artist = track.GetProperty("artists")[0].GetProperty("name").GetString(),
                Album = track.GetProperty("album").GetProperty("name").GetString(),
                Picture = track.GetProperty("album").GetProperty("images")[0].GetProperty("url").GetString(),
                PlayedDate = DateTime.Parse(item.GetProperty("played_at").GetString())
            };
            tracks.Add(song);
        }
        Console.WriteLine(tracks);
        return tracks;
    }

    public async Task<List<Song>> GetTracksByBpmAsync(string accessToken, int bpmTarget, int limit, int bpmThreshold)
    {
        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        var response = await _httpClient.GetAsync($"https://api.spotify.com/v1/me/player/recently-played?limit={limit}");
        if (!response.IsSuccessStatusCode) return null;

        var recentlyPlayedContent = await response.Content.ReadAsStringAsync();
        var recentlyPlayedData = JsonDocument.Parse(recentlyPlayedContent);
        var items = recentlyPlayedData.RootElement.GetProperty("items");

        var trackIds = items.EnumerateArray().Select(item => item.GetProperty("track").GetProperty("id").GetString()).ToList();

        var matchingTracks = new List<JsonElement>();
        foreach (var trackId in trackIds)
        {
            var audioFeaturesResponse = await _httpClient.GetAsync($"https://api.spotify.com/v1/audio-features/{trackId}");
            if (!audioFeaturesResponse.IsSuccessStatusCode) continue;

            var audioFeaturesContent = await audioFeaturesResponse.Content.ReadAsStringAsync();
            var audioFeaturesData = JsonDocument.Parse(audioFeaturesContent);
            if (audioFeaturesData.RootElement.TryGetProperty("tempo", out var tempoElement))
            {
                var tempo = tempoElement.GetDouble();
                if (tempo >= bpmTarget - bpmThreshold && tempo <= bpmTarget + bpmThreshold)
                {
                    foreach (var item in items.EnumerateArray())
                    {
                        var track = item.GetProperty("track");
                        if (track.GetProperty("id").GetString() == trackId)
                        {
                            matchingTracks.Add(item);
                            break;
                        }
                    }
                }
            }
        }

        var tracks = new List<Song>();
        foreach (var item in matchingTracks) 
        {
            var track = item.GetProperty("track");
            var song = new Song
            {
                Id = track.GetProperty("id").GetString(),
                Title = track.GetProperty("name").GetString(),
                Artist = track.GetProperty("artists")[0].GetProperty("name").GetString(),
                Album = track.GetProperty("album").GetProperty("name").GetString(),
                Picture = track.GetProperty("album").GetProperty("images")[0].GetProperty("url").GetString(),
                PlayedDate = DateTime.Parse(item.GetProperty("played_at").GetString())

            };
            tracks.Add(song);
        }
        return tracks;
    }
}

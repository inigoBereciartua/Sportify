using System.Net.Http.Headers;
using System.Text.Json;
using sportify.backend.Models;
using Microsoft.Extensions.Logging;

public class SpotifyService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<SpotifyService> _logger;

    public SpotifyService(HttpClient httpClient, ILogger<SpotifyService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<UserInfo?> GetUserInfoAsync(string accessToken)
    {
        _logger.LogInformation("Fetching user info from Spotify.");

        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        var response = await _httpClient.GetAsync("https://api.spotify.com/v1/me");

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogWarning("Failed to fetch user info. Status code: {StatusCode}", response.StatusCode);
            return null;
        }

        var fetchedResponse = await response.Content.ReadAsStringAsync();
        _logger.LogInformation("Successfully fetched user info from Spotify.");

        var userInfo = JsonDocument.Parse(fetchedResponse);
        var displayName = userInfo.RootElement.GetProperty("display_name").GetString();
        var email = userInfo.RootElement.GetProperty("email").GetString();

        _logger.LogInformation("Parsed user info. Display name: {DisplayName}, Email: {Email}", displayName, email);

        return new UserInfo(displayName, email);
    }

    public async Task<List<Song>> GetRecentlyPlayedTracksAsync(string accessToken, int limit)
    {
        _logger.LogInformation("Fetching recently played tracks with limit {Limit}", limit);

        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        var response = await _httpClient.GetAsync($"https://api.spotify.com/v1/me/player/recently-played?limit={limit}");

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogWarning("Failed to fetch recently played tracks. Status code: {StatusCode}", response.StatusCode);
            return null;
        }

        var recentlyPlayedContent = await response.Content.ReadAsStringAsync();
        _logger.LogInformation("Successfully fetched recently played tracks from Spotify.");

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
                PlayedDate = DateTime.Parse(item.GetProperty("played_at").GetString()),
                Duration = track.GetProperty("duration_ms").GetInt32() / 1000
            };
            tracks.Add(song);
        }

        _logger.LogInformation("Returning {TrackCount} recently played tracks.", tracks.Count);
        return tracks;
    }

    public async Task<List<Song>> GetRecentTracksByBpmAsync(string accessToken, int bpmTarget, int bpmThreshold)
    {
        _logger.LogInformation("Fetching tracks with BPM between {MinBPM} and {MaxBPM}", bpmTarget - bpmThreshold, bpmTarget + bpmThreshold);

        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        var matchingTracks = new List<JsonElement>();
        var pageSize = 50;
    
        // Fetch recently played tracks before the specified timestamp
        var response = await _httpClient.GetAsync($"https://api.spotify.com/v1/me/player/recently-played?limit={pageSize}");

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogWarning("Failed to fetch recently played tracks. Status code: {StatusCode}", response.StatusCode);
            return null;
        }

        var recentlyPlayedContent = await response.Content.ReadAsStringAsync();
        var recentlyPlayedData = JsonDocument.Parse(recentlyPlayedContent);
        var items = recentlyPlayedData.RootElement.GetProperty("items");

        // Log how many items were fetched
        _logger.LogInformation("Fetched {TrackCount} items.", items.GetArrayLength());

        if (!items.EnumerateArray().Any())
        {
            _logger.LogInformation("No more tracks to evaluate, stopping the loop.");
            return new List<Song>();
        }

        var trackIds = items.EnumerateArray().Select(item => item.GetProperty("track").GetProperty("id").GetString()).ToList();
        _logger.LogInformation("Processing {TrackCount} tracks for BPM analysis.", trackIds.Count);

        foreach (var trackId in trackIds)
        {
            var audioFeaturesResponse = await _httpClient.GetAsync($"https://api.spotify.com/v1/audio-features/{trackId}");
            if (!audioFeaturesResponse.IsSuccessStatusCode)
            {
                _logger.LogWarning("Failed to fetch audio features for track {TrackId}. Status code: {StatusCode}", trackId, audioFeaturesResponse.StatusCode);
                continue;
            }

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
                            _logger.LogInformation("Found track with BPM {BPM}.", tempo);
                            break;
                        }
                    }
                }
            }
        }


        _logger.LogInformation("Found {MatchingTrackCount} matching tracks based on BPM.", matchingTracks.Count);

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
                PlayedDate = DateTime.Parse(item.GetProperty("played_at").GetString()),
                Duration = track.GetProperty("duration_ms").GetInt32() / 1000
            };
            tracks.Add(song);
        }

        _logger.LogInformation("Returning {TrackCount} tracks after BPM filtering.", tracks.Count);
        return tracks;
    }

    /**
    * Fetches user tracks from Spotify and filters them based on BPM.
    * @param accessToken The access token for the user.
    * @param bpmTarget The target BPM value.
    * @param bpmThreshold The threshold for BPM values.
    * @param limit The number of tracks to fetch in each request. Max limit (set by Spotify) will be 50.
    * @param totalLimit The total number of tracks to fetch.
    * @return A list of tracks that match the BPM criteria.
    */
    public async Task<List<Song>> GetUserTracksByBpmAsync(string accessToken, int bpmTarget, int bpmThreshold, int limit = 50, int totalLimit = 100)
    {
        _logger.LogInformation("Fetching user tracks with BPM between {MinBPM} and {MaxBPM}, Total limit: {TotalLimit}", bpmTarget - bpmThreshold, bpmTarget + bpmThreshold, totalLimit);

        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        var matchingTracks = new List<JsonElement>();
        var allTracks = new List<JsonElement>();  // Store original track data
        int offset = 0;
        int pageSize = Math.Min(limit, 50);  // Ensure page size is within the allowed range
        int fetchedItems = 0;
        var allTrackIds = new List<string>();

        while (fetchedItems < totalLimit)
        {
            _logger.LogInformation("Fetching tracks with limit {Limit} and offset {Offset}", pageSize, offset);
            var response = await _httpClient.GetAsync($"https://api.spotify.com/v1/me/tracks?limit={pageSize}&offset={offset}");

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Failed to fetch user tracks. Status code: {StatusCode}", response.StatusCode);
                return null;
            }

            var content = await response.Content.ReadAsStringAsync();
            var data = JsonDocument.Parse(content);
            var items = data.RootElement.GetProperty("items");

            _logger.LogInformation("Fetched {TrackCount} tracks with offset {Offset}.", items.GetArrayLength(), offset);

            if (!items.EnumerateArray().Any())
            {
                _logger.LogInformation("No more tracks to evaluate.");
                break;
            }

            // Collect track IDs and store original track data
            foreach (var item in items.EnumerateArray())
            {
                if (item.TryGetProperty("track", out var trackElement))
                {
                    var trackId = trackElement.GetProperty("id").GetString();
                    allTrackIds.Add(trackId);
                    allTracks.Add(item);
                }
                else
                {
                    _logger.LogWarning("Track data missing from item. Skipping this item.");
                }
            }

            fetchedItems += items.GetArrayLength();
            offset += pageSize;

            if (fetchedItems >= totalLimit)
            {
                _logger.LogInformation("Reached the total limit of {TotalLimit} items.", totalLimit);
                break;
            }
        }

        // Fetch audio features in batches (max 100 IDs per batch)
        var tracksWithBpm = new List<JsonElement>();
        for (int i = 0; i < allTrackIds.Count; i += 100)
        {
            var batchTrackIds = allTrackIds.Skip(i).Take(100).ToList();
            var idsString = string.Join(",", batchTrackIds);
            var audioFeaturesResponse = await _httpClient.GetAsync($"https://api.spotify.com/v1/audio-features?ids={idsString}");

            if (!audioFeaturesResponse.IsSuccessStatusCode)
            {
                _logger.LogWarning("Failed to fetch audio features for batch. Status code: {StatusCode}", audioFeaturesResponse.StatusCode);
                continue;
            }

            var audioFeaturesContent = await audioFeaturesResponse.Content.ReadAsStringAsync();
            var audioFeaturesData = JsonDocument.Parse(audioFeaturesContent);

            var audioFeatures = audioFeaturesData.RootElement.GetProperty("audio_features");

            foreach (var audioFeature in audioFeatures.EnumerateArray())
            {
                if (audioFeature.TryGetProperty("tempo", out var tempoElement))
                {
                    var tempo = tempoElement.GetDouble();
                    if (tempo >= bpmTarget - bpmThreshold && tempo <= bpmTarget + bpmThreshold)
                    {
                        tracksWithBpm.Add(audioFeature);
                    }
                }
            }
        }

        // Convert matching tracks to the Song model
        var tracks = new List<Song>();
        foreach (var item in tracksWithBpm)
        {
            var trackId = item.GetProperty("id").GetString();

            // Find the matching track from the original items based on trackId
            var matchingTrackItem = allTracks.FirstOrDefault(trackItem =>
                trackItem.TryGetProperty("track", out var track) && track.GetProperty("id").GetString() == trackId);
            
            if (matchingTrackItem.TryGetProperty("track", out var matchingTrack))
            {

                var song = new Song
                {
                    Id = trackId,
                    Title = matchingTrack.GetProperty("name").GetString(),
                    Artist = matchingTrack.GetProperty("artists")[0].GetProperty("name").GetString(),
                    Album = matchingTrack.GetProperty("album").GetProperty("name").GetString(),
                    Picture = matchingTrack.GetProperty("album").GetProperty("images")[0].GetProperty("url").GetString(),    
                    PlayedDate = DateTime.Parse(matchingTrackItem.GetProperty("added_at").GetString()),  
                    Duration = matchingTrack.GetProperty("duration_ms").GetInt32() / 1000
                };

                tracks.Add(song);
            }
            else
            {
                Console.WriteLine($"Track with ID {trackId} not found.");
            }
        }

        _logger.LogInformation("Returning {TrackCount} tracks after BPM filtering.", tracks.Count);
        return tracks;
    }
}

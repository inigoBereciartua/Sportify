using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using System.Net.Http.Headers;
using System.Text.Json;


var builder = WebApplication.CreateBuilder(args);

// Set port to 5000
builder.WebHost.UseUrls("http://localhost:5000");

// Add services to the container.
builder.Services.AddEndpointsApiExplorer();

builder.Services.AddSwaggerGen();

// Add authorization services
builder.Services.AddAuthorization();

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowVueClient",
        policy =>
        {
            policy.WithOrigins("http://localhost:8080")  // Replace with your VueJS app's URL if it's hosted elsewhere
                  .AllowAnyHeader()
                  .AllowAnyMethod()
                  .AllowCredentials();
        });
});


// Configure Spotify authentication
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = "Cookies";
    options.DefaultSignInScheme = "Cookies";
    options.DefaultChallengeScheme = "Spotify";
})
.AddCookie()  // For storing authentication cookies
.AddSpotify(options =>
{
    // Ensure ClientId and ClientSecret are not null
    options.ClientId = builder.Configuration["Spotify:ClientId"] ?? throw new ArgumentNullException("Spotify:ClientId");
    options.ClientSecret = builder.Configuration["Spotify:ClientSecret"] ?? throw new ArgumentNullException("Spotify:ClientSecret");
    options.CallbackPath = "/signin-spotify";  // Spotify OAuth callback
    options.SaveTokens = true;  // Save tokens for later use
    options.Scope.Add("user-read-email");  // Permissions to access Spotify data
    options.Scope.Add("playlist-modify-public");
    options.Scope.Add("playlist-modify-private");
    options.Scope.Add("user-read-recently-played");
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Enable authentication and authorization middleware
app.UseAuthentication();
app.UseAuthorization();

app.UseCors("AllowVueClient");

app.MapGet("/weatherforecast", () =>
{
    var summaries = new[]
    {
        "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
    };

    var forecast = Enumerable.Range(1, 5).Select(index =>
        new WeatherForecast
        (
            DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
            Random.Shared.Next(-20, 55),
            summaries[Random.Shared.Next(summaries.Length)]
        ))
        .ToArray();

    return forecast;
})
.WithName("GetWeatherForecast")
.WithOpenApi();

// Define login route for Spotify OAuth
app.MapGet("/login", (HttpContext context) => {
    var authenticationProperties = new AuthenticationProperties
    {
        RedirectUri = "http://localhost:8080/callback"  // Redirect back to VueJS app after successful login
    };

    return Results.Challenge(authenticationProperties, new List<string> { "Spotify" });
});

// Define logout route
app.MapGet("/logout", async (HttpContext context) =>
{
    await context.SignOutAsync();
    return Results.Redirect("/");
});

app.MapGet("/userinfo", async (HttpContext context) =>
{
    var accessToken = await context.GetTokenAsync("access_token");

    if (string.IsNullOrEmpty(accessToken))
    {
        return Results.Unauthorized();
    }

    using var httpClient = new HttpClient();
    httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

    var response = await httpClient.GetAsync("https://api.spotify.com/v1/me");

    if (!response.IsSuccessStatusCode)
    {
        Console.WriteLine($"Unsuccessful: {response.IsSuccessStatusCode}");
        return Results.StatusCode((int)response.StatusCode);
    }

    var content = await response.Content.ReadAsStringAsync();
    return Results.Json(content);
});

app.MapGet("/recently-played", async (HttpContext context) => 
{

    var limit = 10;
    var accessToken = await context.GetTokenAsync("access_token");

    if (string.IsNullOrEmpty(accessToken))
    {
        return Results.Unauthorized();
    }

    using var httpClient = new HttpClient();
    httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

    var response = await httpClient.GetAsync($"https://api.spotify.com/v1/me/player/recently-played?limit={limit}");

    if (!response.IsSuccessStatusCode)
    {
        Console.WriteLine($"Unsuccessful: {response.IsSuccessStatusCode}");
        return Results.StatusCode((int)response.StatusCode);
    }

    var content = await response.Content.ReadAsStringAsync();
    return Results.Content(content, "application/json");
});

app.MapGet("/recently-played/bpm", async (HttpContext context) => 
{
    var limit = 20;
    var bpmTarget = 90;  // Target BPM
    var bpmThreshold = 5;  // Allow a +-5 BPM range
    var accessToken = await context.GetTokenAsync("access_token");

    if (string.IsNullOrEmpty(accessToken))
    {
        return Results.Unauthorized();
    }

    using var httpClient = new HttpClient();
    httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

    // Step 1: Fetch the recently played tracks (limit 20)
    var recentlyPlayedResponse = await httpClient.GetAsync($"https://api.spotify.com/v1/me/player/recently-played?limit={limit}");
    
    if (!recentlyPlayedResponse.IsSuccessStatusCode)
    {
        Console.WriteLine($"Unsuccessful: {recentlyPlayedResponse.IsSuccessStatusCode}");
        return Results.StatusCode((int)recentlyPlayedResponse.StatusCode);
    }

    var recentlyPlayedContent = await recentlyPlayedResponse.Content.ReadAsStringAsync();
    var recentlyPlayedData = JsonDocument.Parse(recentlyPlayedContent);
    var items = recentlyPlayedData.RootElement.GetProperty("items");

    var trackIds = new List<string>();

    // Extract track IDs from the recently played songs
    foreach (var item in items.EnumerateArray())
    {
        var trackId = item.GetProperty("track").GetProperty("id").GetString();
        if (!string.IsNullOrEmpty(trackId))
        {
            trackIds.Add(trackId);
        }
    }

    var matchingTracks = new List<JsonElement>();

    // Step 2: Fetch the audio features for each track
    foreach (var trackId in trackIds)
    {
        var audioFeaturesResponse = await httpClient.GetAsync($"https://api.spotify.com/v1/audio-features/{trackId}");

        if (!audioFeaturesResponse.IsSuccessStatusCode)
        {
            Console.WriteLine($"Failed to fetch audio features for track: {trackId}");
            continue;
        }

        var audioFeaturesContent = await audioFeaturesResponse.Content.ReadAsStringAsync();
        var audioFeaturesData = JsonDocument.Parse(audioFeaturesContent);

        if (audioFeaturesData.RootElement.TryGetProperty("tempo", out var tempoElement))
        {
            var tempo = tempoElement.GetDouble();

            // Step 3: Filter tracks based on tempo (BPM)
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

    // Step 4: Return the filtered tracks
    var options = new JsonSerializerOptions { WriteIndented = true };
    var filteredTracksJson = JsonSerializer.Serialize(matchingTracks, options);
    return Results.Content(filteredTracksJson, "application/json");
});




app.Run();

record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
{
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}

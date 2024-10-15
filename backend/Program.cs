using System.Text.Json;

var builder = WebApplication.CreateBuilder(args);

builder.WebHost.UseUrls("http://localhost:5000");

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddAuthorization();

builder.Services.AddControllers();

builder.Configuration.AddEnvironmentVariables();

builder.Services.Configure<RouteOptions>(options =>
{
    options.LowercaseUrls = true;
    options.LowercaseQueryStrings = true;
});

builder.Services.AddControllers().AddJsonOptions(options =>
{
    options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
});


builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowVueClient",
        policy =>
        {
            policy.WithOrigins("http://localhost:8080")
                  .AllowAnyHeader()
                  .AllowAnyMethod()
                  .AllowCredentials();
        });
});

builder.Services.AddHttpClient<SpotifyService>();
builder.Services.AddHttpClient<RunningSessionService>();


builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddDebug();

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = "Cookies";
    options.DefaultSignInScheme = "Cookies";
    options.DefaultChallengeScheme = "Spotify";
})
.AddCookie()
.AddSpotify(options =>
{

    var spotifyClientId = Environment.GetEnvironmentVariable("SPOTIFY_CLIENT_ID");
    var spotifyClientSecret = Environment.GetEnvironmentVariable("SPOTIFY_CLIENT_SECRET");


    if (string.IsNullOrEmpty(spotifyClientId))
    {
        throw new ArgumentNullException("Spotify:ClientId");
    }

    if (string.IsNullOrEmpty(spotifyClientSecret))
    {
        throw new ArgumentNullException("Spotify:ClientSecret");
    }
    options.ClientId = spotifyClientId;
    options.ClientSecret = spotifyClientSecret;
    options.CallbackPath = "/signin-spotify";
    options.SaveTokens = true;
    options.Scope.Add("user-read-email");
    options.Scope.Add("playlist-modify-public");
    options.Scope.Add("playlist-modify-private");
    options.Scope.Add("user-read-recently-played");
    options.Scope.Add("user-library-read");
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseAuthentication();
app.UseAuthorization();
app.UseCors("AllowVueClient");

app.MapControllers();

app.Run("http://0.0.0.0:5000");

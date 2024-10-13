var builder = WebApplication.CreateBuilder(args);

builder.WebHost.UseUrls("http://localhost:5000");

// Add services to the container.
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddAuthorization();

// Add this line to register controllers
builder.Services.AddControllers();  // <-- This is required for controllers to work

builder.Services.Configure<RouteOptions>(options =>
{
    options.LowercaseUrls = true;  // This ensures all routes are lowercase
    options.LowercaseQueryStrings = true;  // This makes query strings lowercase (optional)
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
    options.ClientId = builder.Configuration["Spotify:ClientId"] ?? throw new ArgumentNullException("Spotify:ClientId");
    options.ClientSecret = builder.Configuration["Spotify:ClientSecret"] ?? throw new ArgumentNullException("Spotify:ClientSecret");
    options.CallbackPath = "/signin-spotify";
    options.SaveTokens = true;
    options.Scope.Add("user-read-email");
    options.Scope.Add("playlist-modify-public");
    options.Scope.Add("playlist-modify-private");
    options.Scope.Add("user-read-recently-played");
    options.Scope.Add("user-library-read");
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseAuthentication();
app.UseAuthorization();
app.UseCors("AllowVueClient");

app.MapControllers();  // Enable attribute routing for controllers

app.Run();

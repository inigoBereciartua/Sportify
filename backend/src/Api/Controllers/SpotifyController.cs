using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using sportify.backend.Models;

[ApiController]
[Route("[controller]")]
public class SpotifyController : ControllerBase
{
    private readonly SpotifyService _spotifyService;

    public SpotifyController(SpotifyService spotifyService)
    {
        _spotifyService = spotifyService;
    }

    [HttpGet("userinfo")]
    public async Task<ActionResult<UserInfo>> GetUserInfo()
    {
        var accessToken = await HttpContext.GetTokenAsync("access_token");
        if (string.IsNullOrEmpty(accessToken)) return Unauthorized();

        var userInfo = await _spotifyService.GetUserInfoAsync(accessToken);
        if (userInfo == null) return StatusCode(500);

        return Ok(userInfo);
    }

    [HttpGet("recently-played")]
    public async Task<ActionResult<List<Song>>> GetRecentlyPlayedTracks([FromQuery] int limit = 10)
    {
        var accessToken = await HttpContext.GetTokenAsync("access_token");
        if (string.IsNullOrEmpty(accessToken)) return Unauthorized();

        var tracks = await _spotifyService.GetRecentlyPlayedTracksAsync(accessToken, limit);
        return Ok(tracks);
    }

    [HttpGet("recently-played/bpm")]
    public async Task<ActionResult<List<Song>>> GetRecentlyPlayedTracksByBpm([FromQuery] int bpmTarget = 90)
    {
        var accessToken = await HttpContext.GetTokenAsync("access_token");
        if (string.IsNullOrEmpty(accessToken)) return Unauthorized();

        var tracks = await _spotifyService.GetRecentTracksByBpmAsync(accessToken, bpmTarget, 5);    
        return Ok(tracks);
    }
}

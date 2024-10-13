using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using sportify.backend.Models;

[ApiController]
[Route("[controller]")]
public class RunningSessionController : ControllerBase
{
    private readonly RunningSessionService _runningSessionService;

    public RunningSessionController(RunningSessionService _runningSessionService)
    {
        this._runningSessionService = _runningSessionService;
    }

    [HttpGet("playlist")]
    public async Task<ActionResult<PlaylistProposal>> GetRecentlyPlayedTracks([FromQuery] double pace, [FromQuery] double distance, [FromQuery] int height)
    {
        var accessToken = await HttpContext.GetTokenAsync("access_token");
        if (string.IsNullOrEmpty(accessToken)) return Unauthorized();

        var tracks = await _runningSessionService.GetPlaylistForSession(accessToken, pace, distance, height);
        return Ok(tracks);
    }
}
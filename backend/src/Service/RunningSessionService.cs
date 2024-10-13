using sportify.backend.Models;

public class RunningSessionService
{
    private readonly HttpClient _httpClient;

    private readonly SpotifyService _spotifyService;

    public RunningSessionService(HttpClient httpClient, SpotifyService spotifyService)
    {
        _httpClient = httpClient;
        _spotifyService = spotifyService;
    }

    public async Task<PlaylistProposal> GetPlaylistForSession(string accessToken, double paceInMinPerKm, double distance, int height)
    {
        var bpm = CalculateBPM(paceInMinPerKm, height) / 2;
        var durationInSeconds = paceInMinPerKm * distance * 60;
        Console.WriteLine($"BPM: {bpm}, Duration: {durationInSeconds}");

        const int BPM_THRESHOLD = 10;
        var tracks = await _spotifyService.GetUserTracksByBpmAsync(accessToken, bpm, BPM_THRESHOLD);

        // Need to filter tracks to get needed duration + 40% so that user can skip some tracks
        if (tracks == null || !tracks.Any())
        {
            throw new ArgumentNullException(nameof(tracks), "The collection is null or empty.");
        }

        var totalDurationInSeconds = 0;
        var neededDurationInSeconds = durationInSeconds * 1.4;
        if (tracks.Count > 0)
        {
            totalDurationInSeconds = tracks.Sum(t => t.Duration);
        }

        while (totalDurationInSeconds > neededDurationInSeconds)
        {
            var lastTrack = tracks.Last();
            totalDurationInSeconds -= lastTrack.Duration;
            Console.WriteLine($"Removing track: {lastTrack.Title}, Duration: {lastTrack.Duration}, Total duration: {totalDurationInSeconds}, Needed duration: {neededDurationInSeconds}");
            tracks.Remove(lastTrack);
        }

        Console.WriteLine($"Total duration: {totalDurationInSeconds}, Duration needed: {durationInSeconds}, With +40%: {neededDurationInSeconds}, Song number: {tracks.Count}");

        var name = $"Running Session - {distance}km - {paceInMinPerKm}min/km - {bpm} BPM ";
        var response = new PlaylistProposal(name, bpm, (int)neededDurationInSeconds, tracks);
        return response;
    }

    public static int CalculateBPM(double paceInMinPerKm, double height)
    {
        const double STRIDE_LENGTH_COEFFICIENT = 0.413;  // Approximation factor for stride length
        double metersPerMinute = 1000 / paceInMinPerKm;
        double strideLength = STRIDE_LENGTH_COEFFICIENT * (height / 100);
        double cadence = metersPerMinute / strideLength;

        return (int)Math.Round(cadence);
    }

}
namespace sportify.backend.Models
{
    public class PlaylistProposal
    {
        public string Name { get; set; }
        public int Bpm { get; set; }
        public int NeededDurationInSeconds { get; set; }
        public List<Song> Songs { get; set; }

        public PlaylistProposal(string name, int bpm, int neededDurationInSeconds, List<Song> songs)
        {
            Name = name;
            Bpm = bpm;
            NeededDurationInSeconds = neededDurationInSeconds;
            Songs = songs;
        }
    }
}

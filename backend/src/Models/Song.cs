namespace sportify.backend.Models
{
    public class Song
    {
        public string Id { get; set; }
        public string Title { get; set; }
        public string Artist { get; set; }
        public string Album { get; set; }
        public string Picture { get; set; }
        public DateTime PlayedDate { get; set; }
        public int Duration { get; set; }
    }

}
using System.ComponentModel.DataAnnotations;

namespace sportify.backend.Models
{
    public class NewPlaylist
    {
        [Required]
        public string Name { get; set; }
        public bool Visible { get; set; }
        public bool Colaborative { get; set; }
        public List<string> SongIds { get; set; }

        // Parameterless constructor is needed for deserialization
        public NewPlaylist() {}

        // Optional: You can keep the constructor if you want to create instances manually in code
        public NewPlaylist(string name, bool visible, bool colaborative, List<string> songIds)
        {
            Name = name;
            Visible = visible;
            Colaborative = colaborative;
            SongIds = songIds;
        }
    }
}

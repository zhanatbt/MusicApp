using System.Collections.Generic;

namespace MusicApp.DAL.Models
{
    public class Artist
    {
        public int ArtistId { get; set; }
        public string Name { get; set; }
        public string Bio { get; set; }

        public ICollection<Track> Tracks { get; set; } = new List<Track>();
        public ICollection<Album> Albums { get; set; } = new List<Album>();
    }
}

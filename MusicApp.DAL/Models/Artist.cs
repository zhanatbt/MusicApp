using System.Collections.Generic;

namespace MusicApp.DAL.Models
{
    public class Artist
    {
        public int ArtistId { get; set; }
        public string Name { get; set; }
        public string Bio { get; set; }

        public ICollection<TrackArtist> TrackArtists { get; set; } = new List<TrackArtist>();
        public ICollection<Album> Albums { get; set; } = new List<Album>();
    }
}

using System.Collections.Generic;

namespace MusicApp.DAL.Models
{
    public class Track
    {
        public int TrackId { get; set; }
        public string Title { get; set; }
        public string Genre { get; set; }
        public int DurationSeconds { get; set; }
        public string FilePath { get; set; }  // Path to MP3 file on disk
        public int ArtistId { get; set; }
        public int? AlbumId { get; set; }

        public Artist Artist { get; set; }
        public Album Album { get; set; }
        public ICollection<PlaylistTrack> PlaylistTracks { get; set; } = new List<PlaylistTrack>();
    }
}

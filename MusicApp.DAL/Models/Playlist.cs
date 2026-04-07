using System.Collections.Generic;

namespace MusicApp.DAL.Models
{
    public class Playlist
    {
        public int PlaylistId { get; set; }
        public string Name { get; set; }
        public int UserId { get; set; }

        public User User { get; set; }
        public ICollection<PlaylistTrack> PlaylistTracks { get; set; } = new List<PlaylistTrack>();
    }

    // Junction table — allows same track multiple times in a playlist
    public class PlaylistTrack
    {
        public int PlaylistTrackId { get; set; }  // Surrogate PK to allow duplicates
        public int PlaylistId { get; set; }
        public int TrackId { get; set; }
        public int Position { get; set; }

        public Playlist Playlist { get; set; }
        public Track Track { get; set; }
    }
}

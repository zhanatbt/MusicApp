using System.Collections.Generic;

namespace MusicApp.DAL.Models
{
    public class User
    {
        public int UserId { get; set; }
        public string Username { get; set; }
        public string PasswordHash { get; set; }
        public string Role { get; set; } // "Admin" or "User"

        public ICollection<Playlist> Playlists { get; set; } = new List<Playlist>();
    }
}

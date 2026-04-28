namespace MusicApp.BLL.DTOs
{
    public class TrackDto
    {
        public int TrackId { get; set; }
        public string Title { get; set; }
        public string Genre { get; set; }
        public int DurationSeconds { get; set; }
        public string FilePath { get; set; }

        public List<string> ArtistNames { get; set; } = new List<string>();
        public List<int> ArtistIds { get; set; } = new List<int>();

        public string ArtistsDisplay => ArtistNames.Any()
            ? string.Join(", ", ArtistNames)
            : "— Без исполнителя —";

        public string AlbumTitle { get; set; }
        public int? AlbumId { get; set; }

        public string DurationFormatted =>
            $"{DurationSeconds / 60}:{DurationSeconds % 60:D2}";
    }

    public class PlaylistDto
    {
        public int PlaylistId { get; set; }
        public string Name { get; set; }
        public int UserId { get; set; }
        public int TrackCount { get; set; }
    }

    public class ArtistDto
    {
        public int ArtistId { get; set; }
        public string Name { get; set; }
        public string Bio { get; set; }
    }

    public class AlbumDto
    {
        public int AlbumId { get; set; }
        public string Title { get; set; }
        public int Year { get; set; }
        public int ArtistId { get; set; }
        public string ArtistName { get; set; }
    }

    public class UserDto
    {
        public int UserId { get; set; }
        public string Username { get; set; }
        public string Role { get; set; }
    }

    public class PlaylistTrackItemDto
    {
        public int PlaylistTrackId { get; set; }
        public int Position { get; set; }
        public int TrackId { get; set; }
        public string Title { get; set; }
        public List<string> ArtistNames { get; set; } = new List<string>();
        public string AlbumTitle { get; set; }
        public int DurationSeconds { get; set; }
        public string FilePath { get; set; }

        public string ArtistsDisplay => ArtistNames.Any()
            ? string.Join(", ", ArtistNames)
            : "— Без исполнителя —";

        public string DurationFormatted =>
            $"{DurationSeconds / 60}:{DurationSeconds % 60:D2}";
    }
}

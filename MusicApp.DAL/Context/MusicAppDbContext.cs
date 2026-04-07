using Microsoft.EntityFrameworkCore;
using MusicApp.DAL.Models;

namespace MusicApp.DAL.Context
{
    public class MusicAppDbContext : DbContext
    {
        public DbSet<User> Users { get; set; }
        public DbSet<Artist> Artists { get; set; }
        public DbSet<Album> Albums { get; set; }
        public DbSet<Track> Tracks { get; set; }
        public DbSet<Playlist> Playlists { get; set; }
        public DbSet<PlaylistTrack> PlaylistTracks { get; set; }

        public MusicAppDbContext(DbContextOptions<MusicAppDbContext> options) : base(options) { }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // User
            modelBuilder.Entity<User>(e =>
            {
                e.HasKey(u => u.UserId);
                e.Property(u => u.Username).IsRequired().HasMaxLength(100);
                e.Property(u => u.PasswordHash).IsRequired();
                e.Property(u => u.Role).IsRequired().HasMaxLength(20);
                e.HasIndex(u => u.Username).IsUnique();
            });

            // Artist
            modelBuilder.Entity<Artist>(e =>
            {
                e.HasKey(a => a.ArtistId);
                e.Property(a => a.Name).IsRequired().HasMaxLength(200);
                e.Property(a => a.Bio).HasMaxLength(2000);
            });

            // Album
            modelBuilder.Entity<Album>(e =>
            {
                e.HasKey(a => a.AlbumId);
                e.Property(a => a.Title).IsRequired().HasMaxLength(200);
                e.HasOne(a => a.Artist)
                 .WithMany(ar => ar.Albums)
                 .HasForeignKey(a => a.ArtistId)
                 .OnDelete(DeleteBehavior.Restrict);
            });

            // Track
            modelBuilder.Entity<Track>(e =>
            {
                e.HasKey(t => t.TrackId);
                e.Property(t => t.Title).IsRequired().HasMaxLength(200);
                e.Property(t => t.Genre).HasMaxLength(100);
                e.Property(t => t.FilePath).HasMaxLength(500);

                e.HasOne(t => t.Artist)
                 .WithMany(a => a.Tracks)
                 .HasForeignKey(t => t.ArtistId)
                 .OnDelete(DeleteBehavior.Restrict);

                e.HasOne(t => t.Album)
                 .WithMany(a => a.Tracks)
                 .HasForeignKey(t => t.AlbumId)
                 .OnDelete(DeleteBehavior.SetNull);
            });

            // Playlist
            modelBuilder.Entity<Playlist>(e =>
            {
                e.HasKey(p => p.PlaylistId);
                e.Property(p => p.Name).IsRequired().HasMaxLength(200);
                e.HasIndex(p => new { p.UserId, p.Name }).IsUnique();

                e.HasOne(p => p.User)
                 .WithMany(u => u.Playlists)
                 .HasForeignKey(p => p.UserId)
                 .OnDelete(DeleteBehavior.Cascade);
            });

            // PlaylistTrack
            modelBuilder.Entity<PlaylistTrack>(e =>
            {
                e.HasKey(pt => pt.PlaylistTrackId);

                e.HasOne(pt => pt.Playlist)
                 .WithMany(p => p.PlaylistTracks)
                 .HasForeignKey(pt => pt.PlaylistId)
                 .OnDelete(DeleteBehavior.Cascade);

                e.HasOne(pt => pt.Track)
                 .WithMany(t => t.PlaylistTracks)
                 .HasForeignKey(pt => pt.TrackId)
                 .OnDelete(DeleteBehavior.Cascade);
            });
        }
    }
}

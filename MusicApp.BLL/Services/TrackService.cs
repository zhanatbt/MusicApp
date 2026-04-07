using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using MusicApp.BLL.DTOs;
using MusicApp.BLL.Interfaces;
using MusicApp.DAL.Context;
using MusicApp.DAL.Models;

namespace MusicApp.BLL.Services
{
    public class TrackService : ITrackService
    {
        private readonly MusicAppDbContext _db;

        public TrackService(MusicAppDbContext db) => _db = db;

        private IQueryable<Track> BaseQuery() =>
            _db.Tracks.Include(t => t.Artist).Include(t => t.Album);

        private static TrackDto ToDto(Track t) => new TrackDto
        {
            TrackId       = t.TrackId,
            Title         = t.Title,
            Genre         = t.Genre,
            DurationSeconds = t.DurationSeconds,
            FilePath      = t.FilePath,
            ArtistName    = t.Artist?.Name,
            AlbumTitle    = t.Album?.Title,
            ArtistId      = t.ArtistId,
            AlbumId       = t.AlbumId
        };

        public List<TrackDto> GetAll() =>
            BaseQuery().AsNoTracking().Select(t => ToDto(t)).ToList();  // LINQ — parameterized

        public TrackDto GetById(int id) =>
            ToDto(BaseQuery().AsNoTracking().FirstOrDefault(t => t.TrackId == id));

        // All filters via LINQ — EF generates parameterized SQL automatically
        public List<TrackDto> Search(string title = null, string artist = null,
                                     string album = null, string genre = null)
        {
            var q = BaseQuery().AsNoTracking();

            if (!string.IsNullOrWhiteSpace(title))
                q = q.Where(t => t.Title.Contains(title));
            if (!string.IsNullOrWhiteSpace(artist))
                q = q.Where(t => t.Artist.Name.Contains(artist));
            if (!string.IsNullOrWhiteSpace(album))
                q = q.Where(t => t.Album.Title.Contains(album));
            if (!string.IsNullOrWhiteSpace(genre))
                q = q.Where(t => t.Genre.Contains(genre));

            return q.Select(t => ToDto(t)).ToList();
        }

        public void Add(TrackDto dto)
        {
            _db.Tracks.Add(new Track
            {
                Title           = dto.Title,
                Genre           = dto.Genre,
                DurationSeconds = dto.DurationSeconds,
                FilePath        = dto.FilePath,
                ArtistId        = dto.ArtistId,
                AlbumId         = dto.AlbumId
            });
            _db.SaveChanges();
        }

        public void Update(TrackDto dto)
        {
            var track = _db.Tracks.Find(dto.TrackId)
                        ?? throw new Exception("Трек не найден.");
            track.Title           = dto.Title;
            track.Genre           = dto.Genre;
            track.DurationSeconds = dto.DurationSeconds;
            track.FilePath        = dto.FilePath;
            track.ArtistId        = dto.ArtistId;
            track.AlbumId         = dto.AlbumId;
            _db.SaveChanges();
        }

        public void Delete(int id)
        {
            var track = _db.Tracks.Find(id);
            if (track != null) { _db.Tracks.Remove(track); _db.SaveChanges(); }
        }

        public List<string> GetAllGenres() =>
            _db.Tracks.AsNoTracking()
               .Where(t => t.Genre != null)
               .Select(t => t.Genre)
               .Distinct()
               .OrderBy(g => g)
               .ToList();
    }
}

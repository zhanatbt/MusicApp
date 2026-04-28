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
            _db.Tracks
               .Include(t => t.TrackArtists)
                   .ThenInclude(ta => ta.Artist)
               .Include(t => t.Album);

        private static TrackDto ToDto(Track t) => new TrackDto
        {
            TrackId = t.TrackId,
            Title = t.Title,
            Genre = t.Genre,
            DurationSeconds = t.DurationSeconds,
            FilePath = t.FilePath,
            AlbumTitle = t.Album?.Title,
            AlbumId = t.AlbumId,
            ArtistNames = t.TrackArtists.Select(ta => ta.Artist.Name).ToList(),
            ArtistIds = t.TrackArtists.Select(ta => ta.Artist.ArtistId).ToList()
        };

        public List<TrackDto> GetAll() =>
            BaseQuery()
                .AsNoTracking()
                .AsEnumerable()
                .Select(ToDto)
                .OrderBy(t => t.Title)
                .ToList();

        public TrackDto GetById(int id)
        {
            var track = BaseQuery()
                .AsNoTracking()
                .FirstOrDefault(t => t.TrackId == id);

            return track == null ? null : ToDto(track);
        }

        public List<TrackDto> Search(string title = null, string artist = null,
                                     string album = null, string genre = null)
        {
            var q = BaseQuery().AsNoTracking();

            if (!string.IsNullOrWhiteSpace(title))
                q = q.Where(t => t.Title.Contains(title));

            if (!string.IsNullOrWhiteSpace(artist))
                q = q.Where(t => t.TrackArtists.Any(ta => ta.Artist.Name.Contains(artist)));

            if (!string.IsNullOrWhiteSpace(album))
                q = q.Where(t => t.Album != null && t.Album.Title.Contains(album));

            if (!string.IsNullOrWhiteSpace(genre))
                q = q.Where(t => t.Genre != null && t.Genre.Contains(genre));

            return q.AsEnumerable()
                    .Select(ToDto)
                    .OrderBy(t => t.Title)
                    .ToList();
        }

        public void Add(TrackDto dto)
        {
            if (dto.ArtistIds == null || dto.ArtistIds.Count == 0)
                throw new Exception("Трек должен иметь хотя бы одного исполнителя.");

            var track = new Track
            {
                Title = dto.Title.Trim(),
                Genre = dto.Genre?.Trim(),
                DurationSeconds = dto.DurationSeconds,
                FilePath = dto.FilePath.Trim(),
                AlbumId = dto.AlbumId
            };

            _db.Tracks.Add(track);
            _db.SaveChanges(); // Получаем TrackId

            // Добавляем связи с исполнителями
            foreach (var artistId in dto.ArtistIds.Distinct())
            {
                _db.TrackArtists.Add(new TrackArtist
                {
                    TrackId = track.TrackId,
                    ArtistId = artistId
                });
            }

            _db.SaveChanges();
        }

        public void Update(TrackDto dto)
        {
            var track = _db.Tracks
                           .Include(t => t.TrackArtists)
                           .FirstOrDefault(t => t.TrackId == dto.TrackId)
                       ?? throw new Exception("Трек не найден.");

            if (dto.ArtistIds == null || dto.ArtistIds.Count == 0)
                throw new Exception("Трек должен иметь хотя бы одного исполнителя.");

            // Обновляем основные данные
            track.Title = dto.Title.Trim();
            track.Genre = dto.Genre?.Trim();
            track.DurationSeconds = dto.DurationSeconds;
            track.FilePath = dto.FilePath.Trim();
            track.AlbumId = dto.AlbumId;

            // Удаляем старые связи с исполнителями
            _db.TrackArtists.RemoveRange(track.TrackArtists);

            // Добавляем новые связи
            foreach (var artistId in dto.ArtistIds.Distinct())
            {
                _db.TrackArtists.Add(new TrackArtist
                {
                    TrackId = track.TrackId,
                    ArtistId = artistId
                });
            }

            _db.SaveChanges();
        }

        public void Delete(int id)
        {
            var track = _db.Tracks
                           .Include(t => t.TrackArtists)   // для каскадного удаления
                           .FirstOrDefault(t => t.TrackId == id);

            if (track != null)
            {
                _db.Tracks.Remove(track);
                _db.SaveChanges();
            }
        }

        public List<string> GetAllGenres() =>
            _db.Tracks
               .AsNoTracking()
               .Where(t => t.Genre != null)
               .Select(t => t.Genre)
               .Distinct()
               .OrderBy(g => g)
               .ToList();
    }
}

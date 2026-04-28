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
    public class PlaylistService : IPlaylistService
    {
        private readonly MusicAppDbContext _db;

        public PlaylistService(MusicAppDbContext db) => _db = db;

        public List<PlaylistDto> GetByUser(int userId) =>
            _db.Playlists
               .AsNoTracking()
               .Where(p => p.UserId == userId)
               .Select(p => new PlaylistDto
               {
                   PlaylistId = p.PlaylistId,
                   Name = p.Name,
                   UserId = p.UserId,
                   TrackCount = p.PlaylistTracks.Count
               })
               .OrderBy(p => p.Name)
               .ToList();

        /// <summary>
        /// Возвращает треки плейлиста (для старой совместимости)
        /// </summary>
        public List<TrackDto> GetTracks(int playlistId) =>
            _db.PlaylistTracks
               .AsNoTracking()
               .Where(pt => pt.PlaylistId == playlistId)
               .OrderBy(pt => pt.Position)
               .Include(pt => pt.Track)
                   .ThenInclude(t => t.TrackArtists)
                       .ThenInclude(ta => ta.Artist)
               .Include(pt => pt.Track.Album)
               .Select(pt => new TrackDto
               {
                   TrackId = pt.Track.TrackId,
                   Title = pt.Track.Title,
                   Genre = pt.Track.Genre,
                   DurationSeconds = pt.Track.DurationSeconds,
                   FilePath = pt.Track.FilePath,
                   AlbumTitle = pt.Track.Album != null ? pt.Track.Album.Title : null,
                   AlbumId = pt.Track.AlbumId,
                   ArtistNames = pt.Track.TrackArtists.Select(ta => ta.Artist.Name).ToList(),
                   ArtistIds = pt.Track.TrackArtists.Select(ta => ta.Artist.ArtistId).ToList()
               })
               .ToList();

        /// <summary>
        /// Основной метод — возвращает треки с PlaylistTrackId (используется в UI)
        /// </summary>
        public List<PlaylistTrackItemDto> GetTracksWithIds(int playlistId) =>
            _db.PlaylistTracks
               .AsNoTracking()
               .Where(pt => pt.PlaylistId == playlistId)
               .OrderBy(pt => pt.Position)
               .Include(pt => pt.Track)
                   .ThenInclude(t => t.TrackArtists)
                       .ThenInclude(ta => ta.Artist)
               .Include(pt => pt.Track.Album)
               .Select(pt => new PlaylistTrackItemDto
               {
                   PlaylistTrackId = pt.PlaylistTrackId,
                   Position = pt.Position,
                   TrackId = pt.Track.TrackId,
                   Title = pt.Track.Title,
                   DurationSeconds = pt.Track.DurationSeconds,
                   FilePath = pt.Track.FilePath,
                   AlbumTitle = pt.Track.Album != null ? pt.Track.Album.Title : null,
                   ArtistNames = pt.Track.TrackArtists.Select(ta => ta.Artist.Name).ToList()
               })
               .ToList();

        public void Create(int userId, string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new Exception("Название плейлиста не может быть пустым.");

            if (_db.Playlists.Any(p => p.UserId == userId && p.Name == name))
                throw new Exception($"Плейлист с именем «{name}» уже существует.");

            _db.Playlists.Add(new Playlist { UserId = userId, Name = name.Trim() });
            _db.SaveChanges();
        }

        public void Delete(int playlistId, int userId)
        {
            var pl = _db.Playlists
                         .FirstOrDefault(p => p.PlaylistId == playlistId && p.UserId == userId)
                     ?? throw new Exception("Плейлист не найден или у вас нет прав на его удаление.");

            _db.Playlists.Remove(pl);
            _db.SaveChanges();
        }

        public void AddTrack(int playlistId, int trackId)
        {
            // Проверяем, что трек существует
            if (!_db.Tracks.Any(t => t.TrackId == trackId))
                throw new Exception("Трек не найден.");

            // Проверяем, что трек ещё не добавлен в этот плейлист
            if (_db.PlaylistTracks.Any(pt => pt.PlaylistId == playlistId && pt.TrackId == trackId))
                throw new Exception("Этот трек уже есть в плейлисте.");

            int nextPos = (_db.PlaylistTracks
                              .Where(pt => pt.PlaylistId == playlistId)
                              .Select(pt => (int?)pt.Position)
                              .Max() ?? 0) + 1;

            _db.PlaylistTracks.Add(new PlaylistTrack
            {
                PlaylistId = playlistId,
                TrackId = trackId,
                Position = nextPos
            });

            _db.SaveChanges();
        }

        public void RemoveTrack(int playlistTrackId)
        {
            var pt = _db.PlaylistTracks.Find(playlistTrackId);
            if (pt != null)
            {
                _db.PlaylistTracks.Remove(pt);
                _db.SaveChanges();
            }
        }

        public void MoveTrack(int playlistTrackId, int newPosition)
        {
            var pt = _db.PlaylistTracks.Find(playlistTrackId)
                     ?? throw new Exception("Запись трека в плейлисте не найдена.");

            pt.Position = newPosition;
            _db.SaveChanges();
        }
    }
}

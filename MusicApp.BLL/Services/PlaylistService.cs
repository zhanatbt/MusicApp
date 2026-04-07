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
                   Name       = p.Name,
                   UserId     = p.UserId,
                   TrackCount = p.PlaylistTracks.Count
               }).ToList();

        public List<TrackDto> GetTracks(int playlistId) =>
            _db.PlaylistTracks
               .AsNoTracking()
               .Where(pt => pt.PlaylistId == playlistId)
               .OrderBy(pt => pt.Position)
               .Select(pt => new TrackDto
               {
                   TrackId         = pt.Track.TrackId,
                   Title           = pt.Track.Title,
                   Genre           = pt.Track.Genre,
                   DurationSeconds = pt.Track.DurationSeconds,
                   FilePath        = pt.Track.FilePath,
                   ArtistName      = pt.Track.Artist.Name,
                   AlbumTitle      = pt.Track.Album.Title,
                   ArtistId        = pt.Track.ArtistId,
                   AlbumId         = pt.Track.AlbumId
               }).ToList();

        public void Create(int userId, string name)
        {
            // DB unique index enforces one name per user — EF catches the exception
            if (_db.Playlists.Any(p => p.UserId == userId && p.Name == name))
                throw new Exception($"Плейлист с именем «{name}» уже существует.");

            _db.Playlists.Add(new Playlist { UserId = userId, Name = name });
            _db.SaveChanges();
        }

        public void Delete(int playlistId, int userId)
        {
            var pl = _db.Playlists.FirstOrDefault(p => p.PlaylistId == playlistId && p.UserId == userId)
                     ?? throw new Exception("Плейлист не найден или нет прав.");
            _db.Playlists.Remove(pl);
            _db.SaveChanges();
        }

        public void AddTrack(int playlistId, int trackId)
        {
            int nextPos = _db.PlaylistTracks
                             .Where(pt => pt.PlaylistId == playlistId)
                             .Select(pt => (int?)pt.Position)
                             .Max() ?? 0;
            nextPos++;

            _db.PlaylistTracks.Add(new PlaylistTrack
            {
                PlaylistId = playlistId,
                TrackId    = trackId,
                Position   = nextPos
            });
            _db.SaveChanges();
        }

        public void RemoveTrack(int playlistTrackId)
        {
            var pt = _db.PlaylistTracks.Find(playlistTrackId);
            if (pt != null) { _db.PlaylistTracks.Remove(pt); _db.SaveChanges(); }
        }

        public void MoveTrack(int playlistTrackId, int newPosition)
        {
            var pt = _db.PlaylistTracks.Find(playlistTrackId)
                     ?? throw new Exception("Запись не найдена.");
            pt.Position = newPosition;
            _db.SaveChanges();
        }
    }
}

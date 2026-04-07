using System;
using System.Collections.Generic;
using System.Linq;
using MusicApp.BLL.DTOs;
using MusicApp.BLL.Interfaces;
using MusicApp.DAL.Context;
using MusicApp.DAL.Models;
using Microsoft.EntityFrameworkCore;

namespace MusicApp.BLL.Services
{
    public class ArtistService : IArtistService
    {
        private readonly MusicAppDbContext _db;
        public ArtistService(MusicAppDbContext db) => _db = db;

        public List<ArtistDto> GetAll() =>
            _db.Artists.AsNoTracking()
               .Select(a => new ArtistDto { ArtistId = a.ArtistId, Name = a.Name, Bio = a.Bio })
               .OrderBy(a => a.Name).ToList();

        public void Add(ArtistDto dto)
        {
            _db.Artists.Add(new Artist { Name = dto.Name, Bio = dto.Bio });
            _db.SaveChanges();
        }

        public void Update(ArtistDto dto)
        {
            var a = _db.Artists.Find(dto.ArtistId) ?? throw new Exception("Исполнитель не найден.");
            a.Name = dto.Name;
            a.Bio  = dto.Bio;
            _db.SaveChanges();
        }

        public void Delete(int id)
        {
            var a = _db.Artists.Find(id);
            if (a != null) { _db.Artists.Remove(a); _db.SaveChanges(); }
        }
    }

    public class AlbumService : IAlbumService
    {
        private readonly MusicAppDbContext _db;
        public AlbumService(MusicAppDbContext db) => _db = db;

        public List<AlbumDto> GetAll() =>
            _db.Albums.Include(a => a.Artist).AsNoTracking()
               .Select(a => new AlbumDto
               {
                   AlbumId    = a.AlbumId,
                   Title      = a.Title,
                   Year       = a.Year,
                   ArtistId   = a.ArtistId,
                   ArtistName = a.Artist.Name
               }).OrderBy(a => a.Title).ToList();

        public List<AlbumDto> GetByArtist(int artistId) =>
            _db.Albums.AsNoTracking()
               .Where(a => a.ArtistId == artistId)
               .Select(a => new AlbumDto { AlbumId = a.AlbumId, Title = a.Title, Year = a.Year, ArtistId = a.ArtistId })
               .ToList();

        public void Add(AlbumDto dto)
        {
            _db.Albums.Add(new Album { Title = dto.Title, Year = dto.Year, ArtistId = dto.ArtistId });
            _db.SaveChanges();
        }

        public void Update(AlbumDto dto)
        {
            var a = _db.Albums.Find(dto.AlbumId) ?? throw new Exception("Альбом не найден.");
            a.Title    = dto.Title;
            a.Year     = dto.Year;
            a.ArtistId = dto.ArtistId;
            _db.SaveChanges();
        }

        public void Delete(int id)
        {
            var a = _db.Albums.Find(id);
            if (a != null) { _db.Albums.Remove(a); _db.SaveChanges(); }
        }
    }
}

using System.Collections.Generic;
using MusicApp.BLL.DTOs;

namespace MusicApp.BLL.Interfaces
{
    public interface IAuthService
    {
        UserDto Login(string username, string password);
        void Register(string username, string password, string role = "User");
    }

    public interface ITrackService
    {
        List<TrackDto> GetAll();
        TrackDto GetById(int id);
        List<TrackDto> Search(string title = null, string artist = null,
                              string album = null, string genre = null);
        void Add(TrackDto dto);
        void Update(TrackDto dto);
        void Delete(int id);
        List<string> GetAllGenres();
    }

    public interface IPlaylistService
    {
        List<PlaylistDto> GetByUser(int userId);
        List<TrackDto> GetTracks(int playlistId);
        void Create(int userId, string name);
        void Delete(int playlistId, int userId);
        void AddTrack(int playlistId, int trackId);
        void RemoveTrack(int playlistTrackId);
        void MoveTrack(int playlistTrackId, int newPosition);
    }

    public interface IArtistService
    {
        List<ArtistDto> GetAll();
        void Add(ArtistDto dto);
        void Update(ArtistDto dto);
        void Delete(int id);
    }

    public interface IAlbumService
    {
        List<AlbumDto> GetAll();
        List<AlbumDto> GetByArtist(int artistId);
        void Add(AlbumDto dto);
        void Update(AlbumDto dto);
        void Delete(int id);
    }
}

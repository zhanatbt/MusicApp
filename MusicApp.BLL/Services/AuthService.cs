using System;
using System.Linq;
using MusicApp.BLL.DTOs;
using MusicApp.BLL.Interfaces;
using MusicApp.DAL.Context;
using MusicApp.DAL.Models;

namespace MusicApp.BLL.Services
{
    public class AuthService : IAuthService
    {
        private readonly MusicAppDbContext _db;

        public AuthService(MusicAppDbContext db) => _db = db;

        public UserDto Login(string username, string password)
        {
            // EF parameterized query — no SQL injection possible
            var user = _db.Users.FirstOrDefault(u => u.Username == username);
            if (user == null) return null;

            bool valid = BCrypt.Net.BCrypt.Verify(password, user.PasswordHash);
            if (!valid) return null;

            return new UserDto { UserId = user.UserId, Username = user.Username, Role = user.Role };
        }

        public void Register(string username, string password, string role = "User")
        {
            if (_db.Users.Any(u => u.Username == username))
                throw new Exception("Пользователь с таким именем уже существует.");

            _db.Users.Add(new User
            {
                Username = username,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(password),
                Role = role
            });
            _db.SaveChanges();
        }
    }
}

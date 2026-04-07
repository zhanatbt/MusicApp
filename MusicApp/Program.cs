using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MusicApp.BLL.Interfaces;
using MusicApp.BLL.Services;
using MusicApp.DAL.Context;
using MusicApp.DAL.Models;
using MusicApp.Forms;
using MusicApp.UI.Forms;

namespace MusicApp
{
        static class Program
    {
        public static IServiceProvider Services { get; private set; }
 
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
 
            // ── Configuration (appsettings.json) ─────────────────────────
            var config = new ConfigurationBuilder()
                .SetBasePath(AppContext.BaseDirectory)
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: false)
                .Build();
 
            string connStr = config.GetConnectionString("MusicAppDb")
                ?? throw new InvalidOperationException(
                    "Строка подключения 'MusicAppDb' не найдена в appsettings.json");
 
            // ── Dependency Injection ──────────────────────────────────────
            var collection = new ServiceCollection();
 
            collection.AddDbContext<MusicAppDbContext>(opts =>
                opts.UseSqlServer(connStr),
                ServiceLifetime.Transient);
 
            collection.AddTransient<IAuthService,     AuthService>();
            collection.AddTransient<ITrackService,    TrackService>();
            collection.AddTransient<IPlaylistService, PlaylistService>();
            collection.AddTransient<IArtistService,   ArtistService>();
            collection.AddTransient<IAlbumService,    AlbumService>();
 
            collection.AddTransient<LoginForm>();
            collection.AddTransient<MainForm>();
            collection.AddTransient<AdminForm>();
 
            Services = collection.BuildServiceProvider();
            // ─────────────────────────────────────────────────────────────
 
            // Auto-migrate on startup
            using (var scope = Services.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<MusicAppDbContext>();
                db.Database.Migrate();
 
                if (!db.Users.Any())
                {
                    db.Users.Add(new User
                    {
                        Username     = "admin",
                        PasswordHash = BCrypt.Net.BCrypt.HashPassword("admin123"),
                        Role         = "Admin"
                    });
                    db.SaveChanges();
                }
            }
 
            Application.Run(Services.GetRequiredService<LoginForm>());
        }
    }
}

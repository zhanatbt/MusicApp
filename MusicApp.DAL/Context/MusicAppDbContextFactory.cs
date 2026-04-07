using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace MusicApp.DAL.Context
{
    /// <summary>
    /// Used only by EF Core design-time tools (Add-Migration, Update-Database).
    /// Reads the connection string from appsettings.json of the UI project.
    /// </summary>
    public class MusicAppDbContextFactory : IDesignTimeDbContextFactory<MusicAppDbContext>
    {
        public MusicAppDbContext CreateDbContext(string[] args)
        {
            // Walk up from DAL project bin folder to find appsettings.json in the UI project
            var basePath = Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "MusicApp");
 
            var config = new ConfigurationBuilder()
                .SetBasePath(Path.GetFullPath(basePath))
                .AddJsonFile("appsettings.json", optional: false)
                .Build();
 
            var connStr = config.GetConnectionString("MusicAppDb")
                          ?? throw new InvalidOperationException("Строка подключения не найдена.");
 
            var options = new DbContextOptionsBuilder<MusicAppDbContext>()
                .UseSqlServer(connStr)
                .Options;
 
            return new MusicAppDbContext(options);
        }
    }
}

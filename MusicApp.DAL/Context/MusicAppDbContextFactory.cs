using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace MusicApp.DAL.Context
{
    public class MusicAppDbContextFactory : IDesignTimeDbContextFactory<MusicAppDbContext>
    {
        public MusicAppDbContext CreateDbContext(string[] args)
        {
            var optionsBuilder = new DbContextOptionsBuilder<MusicAppDbContext>();
            optionsBuilder.UseSqlServer(
                @"Server=(localdb)\MSSQLLocalDB;Database=MusicAppDb;Trusted_Connection=True;");
            return new MusicAppDbContext(optionsBuilder.Options);
        }
    }
}

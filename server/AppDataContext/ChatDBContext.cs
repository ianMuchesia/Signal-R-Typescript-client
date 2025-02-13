


using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using server.Models;

namespace server.AppDataContext
{
    public class ChatDBContext : DbContext
    {

        private readonly DbSettings _dbsettings;

        public ChatDBContext(IOptions<DbSettings> dbSettings)
        {
            _dbsettings = dbSettings.Value;
        }

        public DbSet<Message> Messages { get; set; }


        public DbSet<User> Users { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseMySql(_dbsettings.ConnectionString, new MySqlServerVersion(new Version(8, 0, 25)));
        }


    }
}
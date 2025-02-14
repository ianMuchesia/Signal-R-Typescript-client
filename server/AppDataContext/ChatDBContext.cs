


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

        public DbSet<User> Users { get; set; }
        public DbSet<Message> Messages { get; set; }
        public DbSet<Conversation> Conversations { get; set; }


        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseMySql(_dbsettings.ConnectionString, new MySqlServerVersion(new Version(8, 0, 25)));
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Ensure conversation between same users is unique
            modelBuilder.Entity<Conversation>()
                .HasIndex(c => new { c.User1Id, c.User2Id })
                .IsUnique();
        }


    }
}
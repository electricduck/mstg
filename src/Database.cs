using Microsoft.EntityFrameworkCore;
using mstg.Entities;

namespace mstg
{
    public class Database : DbContext
    {
        public DbSet<Media> Medias { get; set; }
        public DbSet<Post> Posts { get; set; }
        public DbSet<Queue> Queue { get; set; }
        public DbSet<User> Users { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
            => optionsBuilder.UseSqlite($"Data Source=config/mstg.db");

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Media>()
                .HasOne(m => m.Post)
                .WithMany(p => p.MediaItems);

            modelBuilder.Entity<Queue>()
                .HasOne(q => q.Post);
        }

        public static void Migrate() {
            var context = new Database();
            context.Database.Migrate();
        }
    }
}
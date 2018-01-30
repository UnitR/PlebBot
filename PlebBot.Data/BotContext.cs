using Microsoft.EntityFrameworkCore;
using PlebBot.Data.Models;

namespace PlebBot.Data
{
    public class BotContext : DbContext
    {
        public virtual DbSet<User> Users { get; set; }
        public virtual DbSet<Server> Servers { get; set; }
        public virtual DbSet<Role> Roles { get; set; }

        public BotContext(DbContextOptions<BotContext> options) : base(options) { }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Server>()
                .Property(s => s.Prefix)
                .HasDefaultValue("!");

            modelBuilder.HasDefaultSchema("public");
            base.OnModelCreating(modelBuilder);
        }
    }
}
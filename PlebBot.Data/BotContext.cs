using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
namespace PlebBot.Data
{
    public class BotContext : DbContext
    {
        public virtual DbSet<User> Users { get; set; }

        public BotContext(DbContextOptions<BotContext> options) : base(options) { }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.HasDefaultSchema("public");
            base.OnModelCreating(modelBuilder);
        }
    }
}
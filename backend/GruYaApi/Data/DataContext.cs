using GruYaApi.Models;
using Microsoft.EntityFrameworkCore;

namespace GruYaApi.Data
{
    public class DataContext : DbContext
    {
        public DataContext(DbContextOptions<DataContext> options)
            : base(options) { }

        public DbSet<User> Users { get; set; }
        public DbSet<Payment> Payments { get; set; }
        public DbSet<Assistance> Assistances { get; set; }
        public DbSet<ProviderProfile> ProviderProfiles { get; set; }
        public DbSet<Vehicle> Vehicles { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Assistance>().OwnsOne(a => a.Location);
            modelBuilder.Entity<ProviderProfile>().OwnsOne(p => p.Location);

            modelBuilder.Entity<Vehicle>(entity =>
            {
                entity.HasOne(v => v.User)
                    .WithMany()
                    .HasForeignKey(v => v.UserId)
                    .OnDelete(DeleteBehavior.Cascade);
            });
        }
    }
}

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
        public DbSet<Quote> Quotes { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Assistance>().OwnsOne(a => a.Origin);
            modelBuilder.Entity<Assistance>().OwnsOne(a => a.Destination);
            modelBuilder.Entity<ProviderProfile>().OwnsOne(p => p.Location);

            modelBuilder.Entity<Vehicle>(entity =>
            {
                entity.HasOne(v => v.User)
                    .WithMany()
                    .HasForeignKey(v => v.UserId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<Quote>(entity =>
            {
                entity.HasOne(q => q.Assistance)
                    .WithMany()
                    .HasForeignKey(q => q.AssistanceId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(q => q.ProviderProfile)
                    .WithMany()
                    .HasForeignKey(q => q.ProviderProfileId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<Assistance>(entity =>
            {
                entity.HasOne(a => a.RequestedProviderProfile)
                    .WithMany()
                    .HasForeignKey(a => a.RequestedProviderProfileId)
                    .OnDelete(DeleteBehavior.SetNull);
            });
        }
    }
}

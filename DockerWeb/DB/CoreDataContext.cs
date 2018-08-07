using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

namespace DockerWeb.DB
{
    public partial class CoreDataContext : DbContext
    {
        public CoreDataContext()
        {
        }

        public CoreDataContext(DbContextOptions<CoreDataContext> options)
            : base(options)
        {
        }

        public virtual DbSet<DataProtectionKeys> DataProtectionKeys { get; set; }


        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<DataProtectionKeys>(entity =>
            {
                entity.HasKey(e => e.FriendlyName);

                entity.ToTable("DataProtectionKeys", "CoreData");

                entity.Property(e => e.FriendlyName)
                    .HasMaxLength(200)
                    .ValueGeneratedNever();

                entity.Property(e => e.XmlData)
                    .IsRequired()
                    .HasMaxLength(5000);
            });
        }
    }
}

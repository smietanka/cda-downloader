using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;

namespace CdaMovieDownloader.EF.Models
{
    public partial class MovieContext : DbContext
    {
        public virtual DbSet<Configuration> Configurations { get; set; }
        public virtual DbSet<Episode> Episodes { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (!optionsBuilder.IsConfigured)
            {
                var dbPath = Path.Combine("X:\\MOJE\\Programowanie\\C#\\CdaMovieDownloader\\CdaMovieDownloader", "cda-db.db");

                optionsBuilder.UseSqlite($"Data Source={dbPath}");
            }
        }
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Configuration>(entity =>
            {
                entity.Property(e => e.MaxQuality).IsRequired();

                entity.Property(e => e.OutputDirectory).IsRequired();

                entity.Property(e => e.Provider).IsRequired();

                entity.Property(e => e.Url).IsRequired();
            });

            modelBuilder.Entity<Episode>(entity =>
            {
                entity.HasIndex(e => e.ConfigurationId, "IX_Episodes_ConfigurationId");

                entity.Property(e => e.ConfigurationId).IsRequired();

                entity.Property(e => e.Name).IsRequired();

                entity.Property(e => e.Url).IsRequired();

                entity.Property(e => e.IsDownloaded).IsRequired()
                    .HasDefaultValue(false);

                entity.Property(c => c.Metadata)
                    .HasConversion(
                    v => JsonSerializer.Serialize(v, (JsonSerializerOptions)null),
                    v => JsonSerializer.Deserialize<Dictionary<string, object>>(v, (JsonSerializerOptions)null));

                entity.HasOne(d => d.Configuration)
                    .WithMany(p => p.Episodes)
                    .HasForeignKey(d => d.ConfigurationId)
                    .OnDelete(DeleteBehavior.ClientSetNull);
            });

            OnModelCreatingPartial(modelBuilder);
        }

        partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
    }
}

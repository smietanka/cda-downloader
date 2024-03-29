﻿using System;
using System.Collections.Generic;
using CdaMovieDownloader.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace CdaMovieDownloader.EF.Models
{
    public partial class MovieContext : DbContext
    {
        public virtual DbSet<Configuration> Configurations { get; set; }
        public virtual DbSet<Episode> Episodes { get; set; }

        public MovieContext(DbContextOptions<MovieContext> options) : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Configuration>(entity =>
            {
                entity.Property(e => e.Id).HasDefaultValueSql("gen_random_uuid()");

                entity.Property(e => e.MaxQuality)
                    .HasConversion(new EnumToStringConverter<Quality>())
                .IsRequired();

                entity.Property(e => e.OutputDirectory).IsRequired();

                entity.Property(e => e.Provider)
                    .HasConversion(new EnumToStringConverter<Provider>())
                .IsRequired();

                entity.Property(e => e.Url).IsRequired();
            });

            modelBuilder.Entity<Episode>(entity =>
            {
                entity.Property(e => e.Id).ValueGeneratedNever();

                entity.Property(e => e.Name).IsRequired();

                entity.Property(e => e.Url).IsRequired();

                entity.HasOne(d => d.Configuration)
                    .WithMany(p => p.Episodes)
                    .HasForeignKey(d => d.ConfigurationId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_ConfigurationId_Configurations_Id");
            });

            OnModelCreatingPartial(modelBuilder);
        }

        partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
    }
}

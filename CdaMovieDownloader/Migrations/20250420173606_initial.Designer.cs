﻿// <auto-generated />
using System;
using CdaMovieDownloader.EF.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

#nullable disable

namespace CdaMovieDownloader.Migrations
{
    [DbContext(typeof(MovieContext))]
    [Migration("20250420173606_initial")]
    partial class Initial
    {
        /// <inheritdoc />
        protected override void BuildTargetModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder.HasAnnotation("ProductVersion", "9.0.4");

            modelBuilder.Entity("CdaMovieDownloader.EF.Models.Configuration", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("TEXT");

                    b.Property<int>("MaxQuality")
                        .HasColumnType("INTEGER");

                    b.Property<string>("OutputDirectory")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.Property<int>("Provider")
                        .HasColumnType("INTEGER");

                    b.Property<string>("Url")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.HasKey("Id");

                    b.ToTable("Configurations");
                });

            modelBuilder.Entity("CdaMovieDownloader.EF.Models.Episode", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("TEXT");

                    b.Property<Guid>("ConfigurationId")
                        .HasColumnType("TEXT");

                    b.Property<string>("DirectUrl")
                        .HasColumnType("TEXT");

                    b.Property<int?>("FileSize")
                        .HasColumnType("INTEGER");

                    b.Property<string>("Name")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.Property<int>("Number")
                        .HasColumnType("INTEGER");

                    b.Property<string>("Url")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.HasKey("Id");

                    b.HasIndex(new[] { "ConfigurationId" }, "IX_Episodes_ConfigurationId");

                    b.ToTable("Episodes");
                });

            modelBuilder.Entity("CdaMovieDownloader.EF.Models.Episode", b =>
                {
                    b.HasOne("CdaMovieDownloader.EF.Models.Configuration", "Configuration")
                        .WithMany("Episodes")
                        .HasForeignKey("ConfigurationId")
                        .IsRequired();

                    b.Navigation("Configuration");
                });

            modelBuilder.Entity("CdaMovieDownloader.EF.Models.Configuration", b =>
                {
                    b.Navigation("Episodes");
                });
#pragma warning restore 612, 618
        }
    }
}

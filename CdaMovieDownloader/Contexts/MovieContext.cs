using CdaMovieDownloader.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CdaMovieDownloader.Contexts
{
    public class MovieContext : DbContext
    {
        private readonly ConfigurationOptions _configuration;
        public MovieContext(DbContextOptions options, IOptions<ConfigurationOptions> configuration) : base(options)
        {
            _configuration = configuration.Value;
        }

        public DbSet<EpisodeDetails> EpisodesDetails { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            base.OnConfiguring(optionsBuilder);
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<EpisodeDetails>();
            base.OnModelCreating(modelBuilder);
        }
    }
}

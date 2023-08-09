using CdaMovieDownloader.Data;
using System;
using System.Collections.Generic;

namespace CdaMovieDownloader.EF.Models
{
    public partial class Configuration
    {
        public Configuration()
        {
            Episodes = new HashSet<Episode>();
        }

        public Guid Id { get; set; }
        public string OutputDirectory { get; set; }
        public string Url { get; set; }
        public Provider Provider { get; set; }
        public Quality MaxQuality { get; set; }

        public virtual ICollection<Episode> Episodes { get; set; }
    }
}

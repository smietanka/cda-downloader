using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace CdaMovieDownloader.EF.Models
{
    public partial class Episode
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public double Number { get; set; }
        public string Url { get; set; }
        public string DirectUrl { get; set; }
        public Guid ConfigurationId { get; set; }
        public int? FileSize { get; set; }
        public bool IsDownloaded { get; set; }

        public Dictionary<string, object> Metadata { get; set; }

        public virtual Configuration Configuration { get; set; }
    }
}

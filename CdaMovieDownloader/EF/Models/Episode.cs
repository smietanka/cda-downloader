using System;
using System.Collections.Generic;

namespace CdaMovieDownloader.EF.Models
{
    public partial class Episode
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public int Number { get; set; }
        public string Url { get; set; }
        public string DirectUrl { get; set; }
        public Guid ConfigurationId { get; set; }
        public int? FileSize { get; set; }

        public virtual Configuration Configuration { get; set; }
    }
}

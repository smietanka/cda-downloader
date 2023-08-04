using Newtonsoft.Json;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace CdaMovieDownloader.Data
{
    [Table("DeviceDetails")]
    public class EpisodeDetails
    {
        [Key]
        public Guid Id { get; set; }
        public string Name { get; set; }
        public int Number { get; set; }
        [JsonPropertyName("CdaUrl")]
        public string Url { get; set; }
        [JsonPropertyName("CdaDirectUrl")]
        public string DirectUrl { get; set; }
        public string AnimeUrl { get; set; }
    }
}

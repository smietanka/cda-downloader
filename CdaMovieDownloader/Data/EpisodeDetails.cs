using Newtonsoft.Json;
using System.Text.Json.Serialization;

namespace CdaMovieDownloader.Data
{
    public class EpisodeDetails
    {
        public string Name { get; set; }
        public int Number { get; set; }
        [JsonPropertyName("CdaUrl")]
        public string Url { get; set; }
        [JsonPropertyName("CdaDirectUrl")]
        public string DirectUrl { get; set; }
    }
}

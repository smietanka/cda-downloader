using System.Collections.Generic;

namespace CdaMovieDownloader.Data
{
    public static class Constants
    {
        public const string FullHD = "1080p";
        public const string HD = "720p";
        public const string MD = "480p";
        public const string LD = "360p";

        public static Dictionary<Quality, string> Qualities = new()
        {
            [Quality.FHD] = FullHD,
            [Quality.HD] = HD,
            [Quality.MD] = MD,
            [Quality.LD] = LD,
        };
    }

    public enum Quality
    {
        FHD = 0,
        HD = 1,
        MD = 2,
        LD = 3
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CdaMovieDownloader.Data
{
    public static class Constants
    {
        public const string FullHD = "1080p";
        public const string HD = "720p";
        public const string MD = "480p";
        public const string LD = "360p";

        public static List<string> Qualities = new List<string>()
        {
            FullHD,
            HD,
            MD
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

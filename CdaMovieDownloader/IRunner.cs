using Spectre.Console;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CdaMovieDownloader
{
    public interface ICrawler
    {
        Task Start(ProgressContext progressContext);
    }
}

using Spectre.Console;
using System.Threading.Tasks;

namespace CdaMovieDownloader
{
    public interface ICrawler
    {
        Task Start(ProgressContext progressContext);
    }
}

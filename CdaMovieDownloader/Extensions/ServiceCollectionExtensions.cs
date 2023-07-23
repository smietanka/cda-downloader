using Microsoft.Extensions.DependencyInjection;
using OpenQA.Selenium.Edge;

namespace CdaMovieDownloader.Extensions
{
    internal static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddEdgeBrowser(this IServiceCollection services) {
            var opts = new EdgeOptions();
            opts.AddArgument("headless");
            opts.AddArgument("log-level=3");
            services.AddSingleton<EdgeOptions>(opts);
            //services.AddSingleton<EdgeDriver>(new EdgeDriver(opts));
            return services;
        }
    }
}

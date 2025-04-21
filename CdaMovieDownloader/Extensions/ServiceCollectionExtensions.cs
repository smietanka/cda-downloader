using Microsoft.Extensions.DependencyInjection;
using OpenQA.Selenium.Edge;

namespace CdaMovieDownloader.Extensions;

internal static class ServiceCollectionExtensions
{
    public static IServiceCollection AddEdgeBrowser(this IServiceCollection services)
    {
        var opts = new EdgeOptions();
        //opts.AddArgument("headless");
        opts.SetLoggingPreference("Warning", OpenQA.Selenium.LogLevel.Warning);
        opts.AddArgument("log-level=1");
        //opts.AddExtension("adblock.crx");
        EdgeDriverService serv = EdgeDriverService.CreateDefaultService();
        serv.HideCommandPromptWindow = true;
        serv.SuppressInitialDiagnosticInformation = true;
        serv.EnableVerboseLogging = false;
        services.AddSingleton(opts);
        services.AddSingleton(serv);
        return services;
    }
}

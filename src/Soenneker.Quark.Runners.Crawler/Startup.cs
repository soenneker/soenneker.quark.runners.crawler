using Microsoft.Extensions.DependencyInjection;
using Soenneker.Git.Util.Registrars;
using Soenneker.Managers.Runners.Registrars;
using Soenneker.Node.Util.Registrars;
using Soenneker.Playwrights.Crawler.Registrars;
using Soenneker.Quark.Runners.Crawler.Utils;
using Soenneker.Quark.Runners.Crawler.Utils.Abstract;
using Soenneker.Utils.File.Download.Registrars;
using Soenneker.Utils.Path.Registrars;

namespace Soenneker.Quark.Runners.Crawler;

/// <summary>
/// Console type startup
/// </summary>
public static class Startup
{
    // This method gets called by the runtime. Use this method to add services to the container.
    public static void ConfigureServices(IServiceCollection services)
    {
        services.SetupIoC();
    }

    public static IServiceCollection SetupIoC(this IServiceCollection services)
    {
        services.AddHostedService<ConsoleHostedService>()
                .AddSingleton<IFileOperationsUtil, FileOperationsUtil>()
                .AddFileDownloadUtilAsSingleton()
                .AddGitUtilAsSingleton()
                .AddRunnersManagerAsSingleton()
                .AddPlaywrightCrawlerAsSingleton()
                .AddPathUtilAsSingleton()
                .AddNodeUtilAsSingleton();

        return services;
    }
}

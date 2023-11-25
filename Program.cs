using GoogleDriveUpdateService.Helpers;
using GoogleDriveUpdateService.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System.Diagnostics;

namespace GoogleDriveUpdateService
{
    public class Program
    {
        static void Main()
        {
            Console.WriteLine("Application starting...");
            var stopwatch = Stopwatch.StartNew();

            var serviceProvider = new ServiceCollection()
                .AddSingleton(Configuration)
                .AddSingleton<IDriveServiceHelper, DriveServiceHelper>()
                .AddSingleton<ICredentialHelper, CredentialHelper>()
                .AddSingleton<IGoogleDriveCRUDHelper, GoogleDriveCRUDHelper>()
                .AddSingleton<ILocalFileHelper, LocalFileHelper>()
                .AddSingleton<IService, Service>()

                .BuildServiceProvider();

            var service = serviceProvider.GetRequiredService<IService>();
            service.MainStartup();

            stopwatch.Stop();
            Console.WriteLine($"Elapsed Time: {stopwatch.Elapsed}");
        }

        static IConfiguration Configuration => new ConfigurationBuilder()
            .SetBasePath(AppContext.BaseDirectory)
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
            .Build();
    }
}
using Microsoft.Extensions.DependencyInjection;
using LocationTracker.Data;
namespace LocationTracker.App;
public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        SQLitePCL.Batteries_V2.Init();
        var builder = MauiApp.CreateBuilder();
        builder.UseMauiApp<App>().UseMauiMaps();
        builder.Services.AddSingleton(new LocationRepository(
            Path.Combine(FileSystem.AppDataDirectory, "locations.db3")));
        builder.Services.AddSingleton<MainPage>();
        return builder.Build();
    }
}

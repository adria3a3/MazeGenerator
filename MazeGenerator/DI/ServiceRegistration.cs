using MazeGenerator.Models;
using MazeGenerator.Rendering;
using MazeGenerator.Services;
using Microsoft.Extensions.DependencyInjection;

namespace MazeGenerator.DI;

public static class ServiceRegistration
{
    public static IServiceProvider BuildServiceProvider(MazeConfiguration config)
    {
        var services = new ServiceCollection();

        // Configuration
        services.AddSingleton(config);

        // Maze generation
        services.AddSingleton<IMazeGenerator>(_ => MazeGeneratorFactory.Create(config.Algorithm, config.Seed));

        // Path finding
        services.AddSingleton<IPathFinder, PathFinder>();
        services.AddSingleton<IEntranceExitSelector, EntranceExitSelector>();

        // Rendering
        services.AddSingleton<MazeWallCalculator>();
        services.AddSingleton<IMazeRenderer>(sp =>
            RendererFactory.Create(config.OutputFormat, sp.GetRequiredService<MazeWallCalculator>()));

        return services.BuildServiceProvider();
    }
}

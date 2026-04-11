using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using EVE.IPH.Domain.Assets.Services;
using EVE.IPH.Domain.Characters.Services;
using EVE.IPH.Domain.Core.Interfaces;
using EVE.IPH.Domain.Industry.Services;
using EVE.IPH.Infrastructure.Data.Connections;
using EVE.IPH.Infrastructure.Data.Repositories.App;
using EVE.IPH.Infrastructure.Data.Repositories.Sde;
using EVE.IPH.Infrastructure.Settings.Storage;
using EVE.IPH.UI.Avalonia.Services;
using EVE.IPH.UI.Avalonia.ViewModels;
using Microsoft.Extensions.DependencyInjection;

namespace EVE.IPH.UI.Avalonia;

public partial class App : Application
{
    private static readonly ServiceProvider Services = ConfigureServices();

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.MainWindow = new MainWindow
            {
                DataContext = Services.GetRequiredService<MainWindowViewModel>(),
            };
        }

        base.OnFrameworkInitializationCompleted();
    }

    private static ServiceProvider ConfigureServices()
    {
        ServiceCollection services = new();

        // The StartupOrchestrator in Program.cs ensures the database exists and all
        // migrations have run before this method is called.
        string databasePath = AppDatabasePath.GetCanonicalDatabasePath();

        services.AddSingleton<IAssetSnapshotHydrator, AssetSnapshotHydrator>();
        services.AddSingleton<IAssetViewFilterService, AssetViewFilterService>();
        services.AddSingleton<IIndustryJobService, IndustryJobService>();
        services.AddSingleton<IIndustryJobPresentationService, IndustryJobPresentationService>();
        services.AddSingleton<IResearchAgentDatacoreService, ResearchAgentDatacoreService>();

        services.AddSingleton<IDbConnectionFactory>(_ => new SqliteConnectionFactory($"Data Source={databasePath}"));
        services.AddSingleton<ICharacterRepository, SqliteCharacterRepository>();
        services.AddSingleton<ICharacterResearchAgentRepository, SqliteCharacterResearchAgentRepository>();
        services.AddSingleton<IResearchAgentDefinitionRepository, SqliteResearchAgentDefinitionRepository>();
        services.AddSingleton<IItemRepository, SqliteItemRepository>();
        services.AddSingleton<ICharacterResearchAgentDataSource, NullCharacterResearchAgentDataSource>();
        services.AddSingleton(TimeProvider.System);
        services.AddSingleton<IResearchAgentService, ResearchAgentService>();

        services.AddSingleton<IPhase11SampleDataProvider, Phase11SampleDataProvider>();
        services.AddSingleton<IAssetsScreenService, AssetsScreenService>();
        services.AddSingleton<IIndustryJobsScreenService, IndustryJobsScreenService>();
        services.AddSingleton<IResearchAgentsScreenService>(provider =>
            new ResearchAgentsScreenService(
                provider.GetRequiredService<IPhase11SampleDataProvider>(),
                provider.GetRequiredService<IResearchAgentDatacoreService>(),
                provider.GetService<ICharacterRepository>(),
                provider.GetService<IResearchAgentService>()));

        services.AddSingleton<AssetsViewModel>();
        services.AddSingleton<IndustryJobsViewModel>();
        services.AddSingleton<ResearchAgentsViewModel>();
        services.AddSingleton<MainWindowViewModel>();

        return services.BuildServiceProvider();
    }
}
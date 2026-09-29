using Applaio.Application.Recruitments;
using Applaio.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;

namespace Applaio.Desktop;

public partial class App : Application
{
    private readonly ServiceProvider _services;
    private Window? _window;

    public App()
    {
        InitializeComponent();

        var services = new ServiceCollection();
        var databasePath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "Applaio",
            "applaio.db");

        Directory.CreateDirectory(Path.GetDirectoryName(databasePath)!);

        services.AddDbContext<ApplaioDbContext>(options =>
            options.UseSqlite($"Data Source={databasePath}"));
        services.AddScoped<IRecruitmentRepository, RecruitmentRepository>();
        services.AddTransient<MainWindow>();

        _services = services.BuildServiceProvider();
    }

    protected override async void OnLaunched(LaunchActivatedEventArgs args)
    {
        await using (var scope = _services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplaioDbContext>();
            await db.Database.EnsureCreatedAsync();
        }

        _window = _services.GetRequiredService<MainWindow>();
        _window.Activate();
    }
}

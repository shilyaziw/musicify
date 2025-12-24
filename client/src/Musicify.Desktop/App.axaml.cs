using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Http;
using Musicify.Core.Abstractions;
using Musicify.Core.Services;
using Musicify.Core.ViewModels;
using Musicify.Desktop.Services;
using Musicify.Desktop.Views;

namespace Musicify.Desktop;

public partial class App : Application
{
    public IServiceProvider Services { get; private set; } = null!;

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);

        // 设置应用程序名称
        Name = "Musicify";

        ConfigureServices();
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            var welcomeViewModel = Services.GetRequiredService<WelcomeViewModel>();
            desktop.MainWindow = new WelcomeWindow
            {
                DataContext = welcomeViewModel
            };
        }

        base.OnFrameworkInitializationCompleted();
    }

    private void ConfigureServices()
    {
        var services = new ServiceCollection();

        // 注册配置 (简化版，实际应该从 appsettings.json 加载)
        var configurationBuilder = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["AI:Provider"] = "OpenAI", // 默认使用 OpenAI，可配置为 Anthropic, Ollama 等
                ["AI:DefaultModel"] = "gpt-4o",
                ["AI:ApiKey"] = "demo-key" // 临时演示用 key，实际使用时需要配置真实的 API Key
            });

        // 环境变量支持需要额外的 NuGet 包
        // 暂时跳过，可以通过配置字典手动添加

        var configuration = configurationBuilder.Build();
        services.AddSingleton<IConfiguration>(configuration);

        // 注册服务
        services.AddSingleton<IFileSystem, FileSystem>();
        services.AddSingleton<IProjectService, ProjectService>();
        services.AddSingleton<INavigationService>(sp => new NavigationService(sp));
        services.AddSingleton<IPromptTemplateService, PromptTemplateService>();
        services.AddSingleton<IMidiAnalysisService, MidiAnalysisService>();
        services.AddSingleton<IRhymeCheckService, RhymeCheckService>();
        services.AddSingleton<IPythonBridgeService, PythonBridgeService>();
        services.AddSingleton<IExportService, ExportService>();
        services.AddSingleton<IFileDialogService, FileDialogService>();

        // 注册 HTTP 客户端工厂
        services.AddHttpClient();

        // 注册 AI 服务
        services.AddSingleton<IAIService>(sp =>
        {
            var httpClientFactory = sp.GetRequiredService<IHttpClientFactory>();
            var httpClient = httpClientFactory.CreateClient();
            var config = sp.GetRequiredService<IConfiguration>();
            var promptService = sp.GetRequiredService<IPromptTemplateService>();
            return new HttpAIService(httpClient, config, promptService);
        });

        // 注册 ViewModels
        services.AddTransient<WelcomeViewModel>();
        services.AddTransient<CreateProjectViewModel>();
        services.AddTransient<MainWindowViewModel>(sp =>
        {
            var projectService = sp.GetRequiredService<IProjectService>();
            var fileSystem = sp.GetRequiredService<IFileSystem>();
            var navigationService = sp.GetService<INavigationService>();
            return new MainWindowViewModel(projectService, fileSystem, navigationService);
        });
        services.AddTransient<LyricsEditorViewModel>(sp =>
        {
            var projectService = sp.GetRequiredService<IProjectService>();
            var fileSystem = sp.GetRequiredService<IFileSystem>();
            var rhymeCheckService = sp.GetService<IRhymeCheckService>();
            return new LyricsEditorViewModel(projectService, fileSystem, rhymeCheckService);
        });
        services.AddTransient<AIChatViewModel>(sp =>
        {
            var aiService = sp.GetRequiredService<IAIService>();
            var projectService = sp.GetRequiredService<IProjectService>();
            var fileSystem = sp.GetRequiredService<IFileSystem>();
            return new AIChatViewModel(aiService, projectService, fileSystem);
        });
        services.AddTransient<MidiAnalysisViewModel>(sp =>
        {
            var midiAnalysisService = sp.GetRequiredService<IMidiAnalysisService>();
            var fileSystem = sp.GetRequiredService<IFileSystem>();
            var fileDialogService = sp.GetService<IFileDialogService>();
            return new MidiAnalysisViewModel(midiAnalysisService, fileSystem, fileDialogService);
        });
        services.AddTransient<ProjectSettingsViewModel>(sp =>
        {
            var projectService = sp.GetRequiredService<IProjectService>();
            var fileSystem = sp.GetRequiredService<IFileSystem>();
            return new ProjectSettingsViewModel(projectService, fileSystem);
        });
        services.AddTransient<ExportViewModel>(sp =>
        {
            var exportService = sp.GetRequiredService<IExportService>();
            var fileSystem = sp.GetRequiredService<IFileSystem>();
            var fileDialogService = sp.GetService<IFileDialogService>();
            return new ExportViewModel(exportService, fileSystem, fileDialogService);
        });

        Services = services.BuildServiceProvider();
    }
}

using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;
using Dignus.DependencyInjection;
using Dignus.DependencyInjection.Extensions;
using Dignus.Log;
using Macro.Infrastructure;
using Macro.Infrastructure.Controller;
using Macro.Infrastructure.Manager;
using Macro.Infrastructure.Models.Updates;
using Macro.Infrastructure.Updates;
using Macro.Models;
using System;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Threading.Tasks;
using Utils;

namespace Macro
{
    public class App : Application
    {
        private HttpClient _updateHttpClient;
        private ApplicationUpdateRestartPresenter _updateRestart;
        private Task _updateCleanupTask = Task.CompletedTask;
        private bool _publishedSingleFile;

        public Window MainWindow
        {
            get
            {
                return ((IClassicDesktopStyleApplicationLifetime)ApplicationLifetime).MainWindow;
            }
        }

        public override void Initialize()
        {
            _publishedSingleFile = typeof(App).Assembly.GetCustomAttributes<AssemblyMetadataAttribute>().Any(attribute => IsPublishedSingleFileMetadata(attribute));
            LogBuilder.Configuration(LoadLogConfiguration());
            LogBuilder.Build();
            Dispatcher.UIThread.UnhandledException += (sender, eventArgs) =>
            {
                LogHelper.Fatal(eventArgs.Exception);
                eventArgs.Handled = true;
            };
            AppDomain.CurrentDomain.UnhandledException += (sender, eventArgs) => LogHelper.Fatal(eventArgs.ExceptionObject as Exception);
            AvaloniaXamlLoader.Load(this);
        }

        public override void OnFrameworkInitializationCompleted()
        {
            if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                DependenciesResolved(desktop);
                desktop.ShutdownMode = ShutdownMode.OnMainWindowClose;
                desktop.MainWindow = new MainWindow();
                _updateCleanupTask = Program.StartUpdateCleanupAsync(desktop.Args);
            }
            base.OnFrameworkInitializationCompleted();
        }

        private static bool IsPublishedSingleFileMetadata(AssemblyMetadataAttribute attribute)
        {
            if (attribute.Key != "MacroPublishedSingleFile")
            {
                return false;
            }
            return attribute.Value == "true";
        }

        private Dignus.Log.Model.LogConfiguration LoadLogConfiguration()
        {
            if (_publishedSingleFile == false)
            {
                if (File.Exists("DignusLog.config") == true)
                {
                    return LogConfigXmlReader.Load("DignusLog.config");
                }
            }
            string temporaryPath = Path.GetTempFileName();
            try
            {
                File.WriteAllText(temporaryPath, PackagedResources.ReadText("Macro.DignusLog.config"));
                return LogConfigXmlReader.Load(temporaryPath);
            }
            finally
            {
                File.Delete(temporaryPath);
            }
        }

        private void DependenciesResolved(IClassicDesktopStyleApplicationLifetime desktop)
        {
            string path = Path.Combine(Environment.CurrentDirectory, ConstHelper.DefaultConfigFile);
            if (File.Exists(path) == false)
            {
                File.WriteAllText(path, JsonHelper.SerializeObject(new Config(), true));
            }
            Config config = JsonHelper.Load<Config>(path);
            _updateHttpClient = new HttpClient(new HttpClientHandler { AllowAutoRedirect = false }) { Timeout = TimeSpan.FromMinutes(30) };
            _updateHttpClient.DefaultRequestHeaders.UserAgent.ParseAdd("DiademMacro/" + VersionNote.CurrentVersion.ToVersionString());
            string version = typeof(App).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>().InformationalVersion;
            ServiceContainer serviceContainer = new();
            serviceContainer.RegisterDependencies(Assembly.GetExecutingAssembly());
            serviceContainer.RegisterDependencies(typeof(MacroExecutionController).Assembly);
            serviceContainer.RegisterType(config);
            serviceContainer.RegisterType<IClassicDesktopStyleApplicationLifetime>(desktop);
            serviceContainer.RegisterType(_updateHttpClient);
            serviceContainer.RegisterType(new ApplicationUpdateEnvironment(version, Environment.ProcessPath, _publishedSingleFile));
            ServiceResolver.SetContainer(serviceContainer);
            serviceContainer.Build();
            _updateRestart = serviceContainer.GetService<ApplicationUpdateRestartPresenter>();
        }

        public async Task CompleteUpdateAfterShutdownAsync()
        {
            try
            {
                await _updateCleanupTask.ConfigureAwait(false);
                if (_updateRestart != null)
                {
                    await _updateRestart.CompleteShutdownAsync().ConfigureAwait(false);
                }
            }
            finally
            {
                _updateHttpClient?.Dispose();
            }
        }
    }
}

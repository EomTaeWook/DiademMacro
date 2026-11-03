using Avalonia;
using Macro.Infrastructure.Updates;
using System;
using System.Globalization;
using System.IO;
using System.Threading.Tasks;

namespace Macro
{
    internal static class Program
    {
        [STAThread]
        public static void Main(string[] arguments)
        {
            if (arguments.Length > 0)
            {
                if (arguments[0] == "--macro-update-helper")
                {
                    Environment.ExitCode = 1;
                    if (arguments.Length == 2)
                    {
                        Environment.ExitCode = ApplicationUpdateHelper.RunHelperAsync(arguments[1]).GetAwaiter().GetResult();
                    }
                    return;
                }
            }
#if WINDOWS
            System.Windows.Forms.Application.SetHighDpiMode(System.Windows.Forms.HighDpiMode.PerMonitorV2);
#else
            if (OperatingSystem.IsMacOS() == false)
            {
                throw new PlatformNotSupportedException("This build requires macOS 14 or later.");
            }
            if (OperatingSystem.IsMacOSVersionAtLeast(14) == false)
            {
                throw new PlatformNotSupportedException("Diadem Macro requires macOS 14 or later.");
            }
            string applicationDataDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "DiademMacro");
            Directory.CreateDirectory(applicationDataDirectory);
            Environment.CurrentDirectory = applicationDataDirectory;
#endif
            BuildAvaloniaApp().StartWithClassicDesktopLifetime(arguments);
            if (Application.Current is App application)
            {
                application.CompleteUpdateAfterShutdownAsync().GetAwaiter().GetResult();
            }
        }

        internal static Task StartUpdateCleanupAsync(string[] arguments)
        {
            if (arguments == null)
            {
                return Task.CompletedTask;
            }
            if (arguments.Length < 3)
            {
                return Task.CompletedTask;
            }
            if (arguments[0] != "--macro-update-cleanup")
            {
                return Task.CompletedTask;
            }
            if (int.TryParse(arguments[2], NumberStyles.None, CultureInfo.InvariantCulture, out int helperProcessId) == false)
            {
                return Task.CompletedTask;
            }
            if (helperProcessId <= 0)
            {
                return Task.CompletedTask;
            }
            return ApplicationUpdateHelper.CleanupAfterHelperAsync(arguments[1], helperProcessId);
        }

        public static AppBuilder BuildAvaloniaApp()
        {
            TemplateDataLoader templateDataLoader = new TemplateDataLoader();
            templateDataLoader.Load();
            return AppBuilder.Configure<App>().UsePlatformDetect().LogToTrace();
        }
    }
}

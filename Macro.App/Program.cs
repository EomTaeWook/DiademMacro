using Avalonia;
using Macro.Infrastructure.Updates;
using System;
using System.Globalization;

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
            StartUpdateCleanup(arguments);
            BuildAvaloniaApp().StartWithClassicDesktopLifetime(arguments);
            if (Application.Current is App application)
            {
                application.CompleteUpdateAfterShutdownAsync().GetAwaiter().GetResult();
            }
        }

        private static void StartUpdateCleanup(string[] arguments)
        {
            if (arguments.Length < 3)
            {
                return;
            }
            if (arguments[0] != "--macro-update-cleanup")
            {
                return;
            }
            if (int.TryParse(arguments[2], NumberStyles.None, CultureInfo.InvariantCulture, out int helperProcessId) == false)
            {
                return;
            }
            if (helperProcessId <= 0)
            {
                return;
            }
            _ = ApplicationUpdateHelper.CleanupAfterHelperAsync(arguments[1], helperProcessId);
        }

        public static AppBuilder BuildAvaloniaApp()
        {
            return AppBuilder.Configure<App>().UsePlatformDetect().LogToTrace();
        }
    }
}

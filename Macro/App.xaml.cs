using DataContainer.Generated;
using Dignus.DependencyInjection;
using Dignus.DependencyInjection.Extensions;
using Dignus.Log;
using Macro.Infrastructure;
using Macro.Infrastructure.Interface;
using Macro.Infrastructure.Manager;
using Macro.Models;
using Macro.Models.ViewModel;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows;
using Utils;

namespace Macro
{
    /// <summary>
    /// App.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            DispatcherUnhandledException += (s, ex) =>
            {
                ex.Handled = true;
                LogHelper.Fatal(ex.Exception);
            };
            AppDomain.CurrentDomain.UnhandledException += (s, ex) =>
            {
                var exception = ex.ExceptionObject as Exception;
                LogHelper.Fatal(exception);
            };
            AppDomain.CurrentDomain.FirstChanceException += (s, ex) =>
            {
                LogHelper.Error(ex.Exception.Message);
            };
            LogBuilder.Configuration(LogConfigXmlReader.Load("DignusLog.config"));
            LogBuilder.Build();
            Init();
            base.OnStartup(e);
        }
        private void Init()
        {
            DependenciesResolved();
            InitTemplate();

            ShutdownMode = ShutdownMode.OnLastWindowClose;
        }
        private void MovePatcherFile()
        {
            DirectoryInfo directoryInfo = new DirectoryInfo(ConstHelper.TempPath);
            if (directoryInfo.Exists == false)
            {
                return;
            }
            foreach (var item in directoryInfo.GetFiles())
            {
                if (item.Extension.ToLower().Equals(".exe") == true)
                {
                    var name = Path.GetFileNameWithoutExtension(item.Name);
                    var processes = Process.GetProcesses().Where(r => r.ProcessName.Equals(name));
                    foreach (var process in processes)
                    {
                        process.Kill();
                    }
                    File.Move(item.FullName, item.Name);
                }
            }
        }
        private void InitTemplate()
        {
            TemplateLoader.Load("Datas");
            TemplateLoader.MakeRefTemplate();
        }
        private void DependenciesResolved()
        {
            var path = Path.Combine(Environment.CurrentDirectory, ConstHelper.DefaultConfigFile);

            if (!File.Exists(path))
            {
                File.WriteAllText(path, JsonHelper.SerializeObject(new Config(), true));
            }

            var config = JsonHelper.Load<Config>(path);
            ServiceContainer serviceContainer = new ServiceContainer();
            serviceContainer.RegisterDependencies(Assembly.GetExecutingAssembly());

            serviceContainer.RegisterType(config);

            serviceContainer.RegisterType<FileService, FileService>();
            serviceContainer.RegisterType<IKeyboardInput, KeyboardInput>();
            serviceContainer.RegisterType<IMouseInput, MouseInput>();

            serviceContainer.RegisterType<EventSettingViewModel, EventSettingViewModel>();
            serviceContainer.RegisterType<SettingViewModel, SettingViewModel>();
            ServiceResolver.SetContainer(serviceContainer);

            serviceContainer.Build();
        }
    }
}
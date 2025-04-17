using DataContainer.Generated;
using Dignus.Coroutine;
using Macro.Extensions;
using Macro.Infrastructure;
using Macro.Infrastructure.Controller;
using Macro.Infrastructure.Manager;
using Macro.Models;
using Macro.Models.Protocols;
using Macro.View;
using MahApps.Metro.Controls;
using MahApps.Metro.Controls.Dialogs;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using Utils;
using Utils.Infrastructure;

namespace Macro
{
    /// <summary>
    /// MainWindow.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class MainWindow : MetroWindow
    {
        private ProcessItem[] _processes;
        private ProcessItem _fixProcess;
        private Config _config;
        private ContentController _contentController;
        private CloseButtonWindow _closeButtonWindow;
        private CoroutineHandler _coroutineHandler = new CoroutineHandler();
        private bool _isShutdownHandled;
        private WebApiManager _webApiManager;
        private AdManager _adManager;
        private ScreenCaptureManager _screenCaptureManager;
        private CacheDataManager _cacheDataManager;
        public MainWindow()
        {
            InitializeComponent();
            Loaded += MainWindow_Loaded;
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            _contentController = ServiceDispatcher.Resolve<ContentController>();
            _config = ServiceDispatcher.Resolve<Config>();
            _cacheDataManager = ServiceDispatcher.Resolve<CacheDataManager>();

            InitEvent();
            Init();
            _closeButtonWindow = new CloseButtonWindow(this, () =>
            {
                AdOverlay.Visibility = Visibility.Collapsed;
            });

            ApplicationManager.Instance.Init();
            _adManager = ServiceDispatcher.Resolve<AdManager>();
            _adManager.InitializeAdUrls();
            if (CheckSponsor() == false)
            {
                _coroutineHandler.Start(ShowAd(true));
            }
            CheckVersion();
        }
        private bool CheckSponsor()
        {
            var response = _webApiManager.Request<CheckSponsorshipResponse>(new CheckSponsorship()
            {
                AccessKey = _config.AccessKey
            });
            if (response == null)
            {
                return false;
            }
            return response.IsSponsor;
        }
        protected override void OnClosing(CancelEventArgs e)
        {
            e.Cancel = true;
            if (_isShutdownHandled)
            {
                return;
            }
            _isShutdownHandled = true;
            _coroutineHandler.Start(ShowAd(false), () =>
            {
                Dispatcher.Invoke(() =>
                {
                    Application.Current.Shutdown();
                });
            });
        }
        private void InitEvent()
        {
            NotifyHelper.ConfigChanged += NotifyHelper_ConfigChanged;
            NotifyHelper.TreeItemOrderChanged += NotifyHelper_TreeItemOrderChanged;
            NotifyHelper.EventTriggerOrderChanged += NotifyHelper_EventTriggerOrderChanged;
            NotifyHelper.SaveEventTriggerModel += NotifyHelper_SaveEventTriggerModel;
            NotifyHelper.DeleteEventTriggerModel += NotifyHelper_DeleteEventTriggerModel;
            NotifyHelper.UpdatedTime += NotifyHelper_UpdatedTime;

            btnSetting.Click += BtnSetting_Click;
            btnGithub.Click += BtnGithub_Click;
            btnStop.Click += BtnStop_Click;
            btnStart.Click += BtnStart_Click;
            btnRefresh.Click += BtnRefresh_Click;

            checkFix.Click += CheckFix_Click;
            comboProcess.SelectionChanged += ComboProcess_SelectionChanged;
        }

        private void BtnRefresh_Click(object sender, RoutedEventArgs e)
        {
            Refresh();
        }

        private void BtnStart_Click(object sender, RoutedEventArgs e)
        {
            Dispatcher.Invoke(() =>
            {
                var buttons = this.FindChildren<Button>();
                foreach (var button in buttons)
                {
                    if (button.Equals(btnStart) || button.Equals(btnStop))
                    {
                        continue;
                    }
                    button.IsEnabled = false;
                }
                comboProcess.IsEnabled = false;
                checkFix.IsEnabled = false;
                btnStop.Visibility = Visibility.Visible;
                btnStart.Visibility = Visibility.Collapsed;
            });
            
        }

        private void BtnStop_Click(object sender, RoutedEventArgs e)
        {
            ApplicationManager.ShowProgressbar();
            _contentController.Stop();
            Dispatcher.Invoke(() =>
            {
                var buttons = this.FindChildren<Button>();
                foreach (var button in buttons)
                {
                    if (button.Equals(btnStart) || button.Equals(btnStop))
                    {
                        continue;
                    }
                    button.IsEnabled = true;
                }
                comboProcess.IsEnabled = true;
                checkFix.IsEnabled = true;
                btnStart.Visibility = Visibility.Visible;
                btnStop.Visibility = Visibility.Collapsed;
            });
            ApplicationManager.HideProgressbar();
        }

        private void CheckFix_Click(object sender, RoutedEventArgs e)
        {
            if (checkFix.IsChecked == true)
            {
                _fixProcess = comboProcess.SelectedItem as ProcessItem;
            }
            else
            {
                _fixProcess = null;
            }
        }

        private void BtnGithub_Click(object sender, RoutedEventArgs e)
        {
            Process.Start(ConstHelper.HelpUrl);
        }

        private void BtnSetting_Click(object sender, RoutedEventArgs e)
        {
            UIManager.Instance.AddPopup<SettingView>();
        }

        private void NotifyHelper_UpdatedTime(UpdatedTimeArgs obj)
        {
            _coroutineHandler.UpdateCoroutines(obj.DeltaTime);
        }

        private void MainWindow_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == System.Windows.Input.Key.Escape)
            {
                BtnStop_Click(btnStop, null);
            }
        }

        private void Init()
        {
            _webApiManager = ServiceDispatcher.Resolve<WebApiManager>();

            if (Environment.OSVersion.Version >= new System.Version(6, 1, 0))
            {
                if (Environment.OSVersion.Version >= new System.Version(10, 0, 15063))
                {
                    NativeHelper.SetProcessDpiAwarenessContext(PROCESS_DPI_AWARENESS.PROCESS_DPI_AWARENESS_CONTEXT_PER_MONITOR_AWARE_V2);
                }
                else
                {
                    NativeHelper.SetProcessDpiAwareness(PROCESS_DPI_AWARENESS.PROCESS_PER_MONITOR_DPI_AWARE);
                }
            }
            else
            {
                var template = TemplateContainer<MessageTemplate>.Find(1006);
                ApplicationManager.ShowMessageDialog("Error", template.GetString());
                Process.GetCurrentProcess().Kill();
            }
            Refresh();
            LoadSaveFile(GetSaveFilePath());
        }
        private string GetSaveFilePath()
        {
            var path = ConstHelper.DefaultSavePath;

            if(string.IsNullOrEmpty(_config.SavePath) == false)
            {
                path = _config.SavePath;
            }
            return Path.Combine(path, ConstHelper.DefaultSaveFileName);
        }

        private void Refresh()
        {
            if (Directory.Exists(GetSaveFilePath()) == false)
            {
                Directory.CreateDirectory(GetSaveFilePath());
            }

            _processes = Process.GetProcesses()
                .Where(r => r.MainWindowHandle != IntPtr.Zero)
                .Select(r => new ProcessItem(r))
                .OrderBy(r => r.ProcessName).ToArray();

            comboProcess.ItemsSource = _processes;
            comboProcess.DisplayMemberPath = "ProcessName";
            comboProcess.SelectedValuePath = "Process";
        }


        private void NotifyHelper_DeleteEventTriggerModel(DeleteEventTriggerModelArgs obj)
        {

        }

        private void NotifyHelper_SaveEventTriggerModel(SaveEventTriggerModelArgs obj)
        {

        }
        private void Save()
        {
            var fileService = ServiceDispatcher.Resolve<FileService>();
        }
        private void NotifyHelper_TreeItemOrderChanged(EventTriggerOrderChangedEventArgs e)
        {
            if (File.Exists(GetSaveFilePath()))
            {
                File.Delete(GetSaveFilePath());
            }
            Save();
        }
        private void NotifyHelper_EventTriggerOrderChanged(EventTriggerOrderChangedEventArgs obj)
        {
            obj.SelectedTreeViewItem.IsSelected = true;
            Save();
        }

        private void ComboProcess_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            CheckFix_Click(checkFix, null);
        }
        private void NotifyHelper_SelectTreeViewChanged(SelctTreeViewItemChangedEventArgs e)
        {

        }

        public void LoadSaveFile(string path)
        {
            var fileManager = ServiceDispatcher.Resolve<FileService>();
            var loadDatas = fileManager.Load<EventTriggerModel>(path);
            if (loadDatas == null)
            {
                loadDatas = new List<EventTriggerModel>();
            }
            _cacheDataManager.InitDatas(loadDatas);
        }

        private void NotifyHelper_ConfigChanged(ConfigEventArgs e)
        {
            ApplicationManager.ShowProgressbar();
            _config = e.Config;
            Refresh();
            LoadSaveFile(GetSaveFilePath());
            ApplicationManager.HideProgressbar();
        }
        private void CheckVersion()
        {
            if (_config.VersionCheck == false)
            {
                return;
            }

            var response = _webApiManager.Request<GetMacroLatestVersionResponse>(new GetMacroLatestVersion());
            if (response == null)
            {
                return;
            }

            var latestNote = response.VersionNote;

            if (latestNote.Version > VersionNote.CurrentVersion)
            {
                var newVersionTemplate = TemplateContainer<MessageTemplate>.Find(1011);

                if (ApplicationManager.ShowMessageDialog("Information", $"{newVersionTemplate.GetString()}", MessageDialogStyle.AffirmativeAndNegative) == MessageDialogResult.Affirmative)
                {
                    Process.Start(ConstHelper.VersionInfoPageUrl);
                }
            }
        }
        private IEnumerator ShowAd(bool isCloseButtonShow)
        {
#if DEBUG
            yield break;
#endif
            Dispatcher.Invoke(async () =>
            {
                AdOverlay.Visibility = Visibility.Visible;
                await EmbeddedWebView.LoadUrlAsync(_adManager.GetRandomAdUrl());
            });

            yield return new DelayInSeconds(3.5F);

            if (isCloseButtonShow)
            {
                Dispatcher.Invoke(() =>
                {
                    _closeButtonWindow.Show();
                });
            }
        }
    }
}

using DataContainer.Generated;
using Dignus.Coroutine;
using Macro.Extensions;
using Macro.Infrastructure;
using Macro.Infrastructure.Controller;
using Macro.Infrastructure.Manager;
using Macro.Models;
using Macro.Models.Protocols;
using Macro.Models.ViewModel;
using Macro.UI;
using Macro.View;
using MahApps.Metro.Controls;
using MahApps.Metro.Controls.Dialogs;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
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
        private Config _config;
        private CloseButtonWindow _closeButtonWindow;
        private readonly CoroutineHandler _coroutineHandler = new CoroutineHandler();
        private bool _isShutdownHandled;
        private WebApiManager _webApiManager;
        private AdManager _adManager;
        private CacheDataManager _cacheDataManager;
        private bool _isEventItemMoveButtonPressed;
        public MainWindow()
        {
            InitializeComponent();
            Loaded += MainWindow_Loaded;
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            _config = ServiceResolver.GetService<Config>();
            _cacheDataManager = ServiceResolver.GetService<CacheDataManager>();

            InitEvent();
            Init();
            _closeButtonWindow = new CloseButtonWindow(this, () =>
            {
                AdOverlay.Visibility = Visibility.Collapsed;
            });

            ApplicationManager.Instance.Init();
            _adManager = ServiceResolver.GetService<AdManager>();
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
            NotifyHelper.ScreenCaptureCompleted += ScreenCaptureCompleted;

            btnSetting.Click += BtnSetting_Click;
            btnGithub.Click += BtnGithub_Click;
            btnStop.Click += BtnStop_Click;
            btnStart.Click += BtnStart_Click;
            btnRefresh.Click += BtnRefresh_Click;
            btnAddEventItem.Click += BtnAddEventItem_Click;
            btnRemoveEventItem.Click += BtnRemoveEventItem_Click;
            btnCopyEventItem.Click += BtnCopyEventItem_Click;
            btnDownEventItem.PreviewMouseDown += BtnDownEventItem_PreviewMouseDown;
            btnDownEventItem.PreviewMouseUp += BtnDownEventItem_PreviewMouseUp;
            btnUpEventItem.PreviewMouseDown += BtnUpEventItem_PreviewMouseDown;
            btnUpEventItem.PreviewMouseUp += BtnUpEventItem_PreviewMouseUp;
            btnOptionEventItem.Click += BtnOptionEventItem_Click;

            checkFix.Click += CheckFix_Click;
            comboProcess.SelectionChanged += ComboProcess_SelectionChanged;

            this.eventListView.treeGridView.SelectedItemChanged += TreeGridView_SelectedItemChanged;

            this.eventListView.Loaded += EventListView_Loaded;
            this.KeyDown += MainWindow_KeyDown;
        }

        private void BtnOptionEventItem_Click(object sender, RoutedEventArgs e)
        {
            var selectionStateController = ServiceResolver.GetService<SelectionStateController>();

            if (selectionStateController.SelectTreeGridViewItem == null)
            {
                return;
            }

            var eventTriggerModel = selectionStateController.SelectTreeGridViewItem.DataContext<EventTriggerModel>();

            var optionDialog = new OptionDialog
            {
                Owner = Application.Current.MainWindow
            };
            optionDialog.Init(eventTriggerModel);

            bool? result = optionDialog.ShowDialog();
        }

        private void BtnUpEventItem_PreviewMouseUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            _isEventItemMoveButtonPressed = false;
        }

        private void BtnUpEventItem_PreviewMouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            var selectionStateController = ServiceResolver.GetService<SelectionStateController>();

            if (selectionStateController.SelectTreeGridViewItem == null)
            {
                return;
            }

            var viewModel = eventListView.DataContext<EventListViewModel>();

            var selectTreeGridViewItem = selectionStateController.SelectTreeGridViewItem;

            var item = selectTreeGridViewItem.DataContext<EventTriggerModel>();

            var currentIndex = viewModel.EventItems.IndexOf(item);
            if (currentIndex <= 0)
            {
                return;
            }

            viewModel.EventItems.Move(currentIndex, currentIndex - 1);

            CoroutineRunner.Start(2, RepeatMoveEventItemCoroutine(btnDownEventItem, selectionStateController.SelectTreeGridViewItem));
        }

        private void BtnDownEventItem_PreviewMouseUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            _isEventItemMoveButtonPressed = false;
        }

        private void BtnDownEventItem_PreviewMouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            var selectionStateController = ServiceResolver.GetService<SelectionStateController>();

            if (selectionStateController.SelectTreeGridViewItem == null)
            {
                return;
            }

            var viewModel = eventListView.DataContext<EventListViewModel>();

            var selectTreeGridViewItem = selectionStateController.SelectTreeGridViewItem;

            var item = selectTreeGridViewItem.DataContext<EventTriggerModel>();

            var currentIndex = viewModel.EventItems.IndexOf(item);
            if (currentIndex >= viewModel.EventItems.Count - 1)
            {
                return;
            }

            viewModel.EventItems.Move(currentIndex, currentIndex + 1);

            CoroutineRunner.Start(2, RepeatMoveEventItemCoroutine(btnDownEventItem, selectionStateController.SelectTreeGridViewItem));
        }

        private IEnumerator RepeatMoveEventItemCoroutine(object sender, TreeGridViewItem selectTreeGridViewItem)
        {
            if (_isEventItemMoveButtonPressed == false)
            {
                yield break;
            }
            int moveDirection;
            if (sender.Equals(btnUpEventItem))
            {
                moveDirection = -1;
            }
            else if (sender.Equals(btnDownEventItem))
            {
                moveDirection = 1;
            }
            else
            {
                yield break;
            }

            var viewModel = eventListView.DataContext<EventListViewModel>();
            var item = selectTreeGridViewItem.DataContext<EventTriggerModel>();

            while (_isEventItemMoveButtonPressed)
            {
                var currentIndex = viewModel.EventItems.IndexOf(item);

                if (currentIndex <= 0 && moveDirection == -1)
                {
                    yield break;
                }
                else if (currentIndex >= viewModel.EventItems.Count - 1 && moveDirection == 1)
                {
                    yield break;
                }
                viewModel.EventItems.Move(currentIndex, currentIndex + moveDirection);

                yield return 0.3F;
            }
        }

        private void EventListView_Loaded(object sender, RoutedEventArgs e)
        {
            LoadSaveFile(GetSaveFilePath());
        }

        private void TreeGridView_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            if (sender is UI.TreeGridView treeGridView == false)
            {
                return;
            }
            if (treeGridView.SelectedItem is EventTriggerModel == false)
            {
                return;
            }

            var treeGridViewItem = treeGridView.GetSelectItemFromObject<TreeGridViewItem>(treeGridView.SelectedItem);

            var selectionStateController = ServiceResolver.GetService<SelectionStateController>();

            selectionStateController.SelectTreeGridViewItem = treeGridViewItem;

            RefreshEventItemButton();
        }

        private void RefreshEventItemButton()
        {
            var selectionStateController = ServiceResolver.GetService<SelectionStateController>();

            if (selectionStateController.SelectTreeGridViewItem != null)
            {
                btnAddEventItem.Visibility = Visibility.Collapsed;
                btnRemoveEventItem.Visibility = Visibility.Visible;
                btnCopyEventItem.Visibility = Visibility.Visible;
                btnUpEventItem.Visibility = Visibility.Visible;
                btnDownEventItem.Visibility = Visibility.Visible;
                btnOptionEventItem.Visibility = Visibility.Visible;
            }
            else
            {
                btnAddEventItem.Visibility = Visibility.Visible;
                btnRemoveEventItem.Visibility = Visibility.Collapsed;
                btnCopyEventItem.Visibility = Visibility.Collapsed;
                btnUpEventItem.Visibility = Visibility.Collapsed;
                btnDownEventItem.Visibility = Visibility.Collapsed;
                btnOptionEventItem.Visibility = Visibility.Collapsed;
            }
        }

        private ProcessInfo GetSelectedProcessInfo()
        {
            if (comboProcess.SelectedValue as Process == null)
            {
                return null;
            }

            var rect = new IntRect();
            NativeHelper.GetWindowRect((comboProcess.SelectedValue as Process).MainWindowHandle, ref rect);

            var processInfo = new ProcessInfo()
            {
                ProcessName = (comboProcess.SelectedValue as Process).ProcessName,
                Position = rect
            };
            return processInfo;
        }
        private void ScreenCaptureCompleted(CaptureCompletedEventArgs obj)
        {
            ApplicationManager.Instance.CloseCaptureView();
            var capture = obj.CaptureImage;

            if (capture == null)
            {
                return;
            }

            var processInfo = GetSelectedProcessInfo();

            if (processInfo == null)
            {
                var labelTemplate = TemplateContainer<LabelTemplate>.Find(1062);
                var messageTemplate = TemplateContainer<MessageTemplate>.Find(1017);
                ApplicationManager.ShowMessageDialog(labelTemplate.GetString(), messageTemplate.GetString());
                return;
            }

            var viewModel = eventListView.DataContext<EventListViewModel>();

            var eventModel = new EventTriggerModel
            {
                Image = new Bitmap(obj.CaptureImage, capture.Width, capture.Height),
                MonitorInfo = obj.MonitorInfo,
                ProcessInfo = processInfo,
            };
            viewModel.EventItems.Add(eventModel);

            _cacheDataManager.MakeIndexTriggerModel(eventModel);

            Save();
        }

        private void BtnCopyEventItem_Click(object sender, RoutedEventArgs e)
        {

        }

        private void BtnRemoveEventItem_Click(object sender, RoutedEventArgs e)
        {
            var selectionStateController = ServiceResolver.GetService<SelectionStateController>();

            if (selectionStateController.SelectTreeGridViewItem == null)
            {
                return;
            }

            var viewModel = eventListView.treeGridView.DataContext<EventListViewModel>();
            var eventItem = selectionStateController.SelectTreeGridViewItem.DataContext<EventTriggerModel>();
            viewModel.EventItems.Remove(eventItem);
            selectionStateController.UnselectTreeGridViewItem();

            RefreshEventItemButton();

            Save();
        }

        private void BtnAddEventItem_Click(object sender, RoutedEventArgs e)
        {
            if (comboProcess.SelectedItem == null)
            {
                var labelTemplate = TemplateContainer<LabelTemplate>.Find(1062);
                var messageTemplate = TemplateContainer<MessageTemplate>.Find(1017);
                ApplicationManager.ShowMessageDialog(labelTemplate.GetString(), messageTemplate.GetString());
                return;
            }
            ApplicationManager.Instance.ShowCaptureImageView();
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
            //_contentController.Stop();
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
            var selectionStateController = ServiceResolver.GetService<SelectionStateController>();

            if (checkFix.IsChecked == true)
            {
                selectionStateController.SelectProcessItem = comboProcess.SelectedItem as ProcessItem;
            }
            else
            {
                selectionStateController.SelectProcessItem = null;
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

        private void MainWindow_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == System.Windows.Input.Key.Escape)
            {
                var selectionStateController = ServiceResolver.GetService<SelectionStateController>();
                selectionStateController.UnselectTreeGridViewItem();

                RefreshEventItemButton();
                BtnStop_Click(btnStop, null);
            }
        }

        private void Init()
        {
            _webApiManager = ServiceResolver.GetService<WebApiManager>();

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
        }
        private string GetSaveFilePath()
        {
            var path = ConstHelper.DefaultSavePath;

            if (string.IsNullOrEmpty(_config.SavePath) == false)
            {
                path = _config.SavePath;
            }
            return Path.Combine(path, ConstHelper.DefaultSaveFileName);
        }
        private string GetSaveDirectoryPath()
        {
            if (string.IsNullOrEmpty(_config.SavePath) == true)
            {
                return ConstHelper.DefaultSavePath;
            }
            return _config.SavePath;
        }

        private void Refresh()
        {
            if (Directory.Exists(GetSaveDirectoryPath()) == false)
            {
                Directory.CreateDirectory(GetSaveDirectoryPath());
            }

            _processes = Process.GetProcesses()
                .Where(r => r.MainWindowHandle != IntPtr.Zero)
                .Select(r => new ProcessItem(r))
                .OrderBy(r => r.ProcessName).ToArray();

            comboProcess.ItemsSource = _processes;
            comboProcess.DisplayMemberPath = "ProcessName";
            comboProcess.SelectedValuePath = "Process";
        }
        private void Save()
        {
            var viewModel = eventListView.DataContext<EventListViewModel>();

            var fileService = ServiceResolver.GetService<FileService>();

            fileService.Save(GetSaveFilePath(), viewModel.EventItems);
        }

        private void ComboProcess_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            CheckFix_Click(checkFix, null);
        }

        public void LoadSaveFile(string path)
        {
            var fileManager = ServiceResolver.GetService<FileService>();
            var loadDatas = fileManager.Load<EventTriggerModel>(path);
            if (loadDatas == null)
            {
                loadDatas = new List<EventTriggerModel>();
            }
            _cacheDataManager.InitDatas(loadDatas);
            var model = eventListView.DataContext<EventListViewModel>();
            model.EventItems.Clear();

            foreach (var item in loadDatas)
            {
                model.EventItems.Add(item);
            }
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

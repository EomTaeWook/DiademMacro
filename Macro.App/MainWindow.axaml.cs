using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Threading;
using DataContainer.Generated;
using Dignus.Collections;
using Dignus.Coroutine;
using Dignus.Log;
using Macro.Extensions;
using Macro.Infrastructure;
using Macro.Infrastructure.Controller;
using Macro.Infrastructure.Manager;
using Macro.Infrastructure.Updates;
using Macro.Models;
using Macro.Models.Converters;
using Macro.Models.Protocols;
using Macro.Models.ViewModel;
using Macro.UI;
using Macro.View;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Utils;
using Utils.Infrastructure;
using ProcessInfo = Macro.Models.ProcessInfo;

namespace Macro
{
    public class MainWindow : Window
    {
        internal Avalonia.Controls.Button btnGithub;
        internal Avalonia.Controls.Button btnSetting;
        internal Avalonia.Controls.ComboBox comboProcess;
        internal Avalonia.Controls.Button btnRefresh;
        internal Avalonia.Controls.CheckBox checkFix;
        internal Avalonia.Controls.Button btnStart;
        internal Avalonia.Controls.Button btnStop;
        internal Avalonia.Controls.Button btnAddEventItem;
        internal Avalonia.Controls.Button btnRemoveEventItem;
        internal Avalonia.Controls.Button btnCopyEventItem;
        internal Avalonia.Controls.Button btnUpEventItem;
        internal Avalonia.Controls.Button btnDownEventItem;
        internal Avalonia.Controls.Button btnOptionEventItem;
        internal Macro.View.EventListView eventListView;

        private void InitializeComponent()
        {
            Avalonia.Markup.Xaml.AvaloniaXamlLoader.Load(this);
            btnGithub = this.FindControl<Avalonia.Controls.Button>("btnGithub");
            btnSetting = this.FindControl<Avalonia.Controls.Button>("btnSetting");
            comboProcess = this.FindControl<Avalonia.Controls.ComboBox>("comboProcess");
            btnRefresh = this.FindControl<Avalonia.Controls.Button>("btnRefresh");
            checkFix = this.FindControl<Avalonia.Controls.CheckBox>("checkFix");
            btnStart = this.FindControl<Avalonia.Controls.Button>("btnStart");
            btnStop = this.FindControl<Avalonia.Controls.Button>("btnStop");
            btnAddEventItem = this.FindControl<Avalonia.Controls.Button>("btnAddEventItem");
            btnRemoveEventItem = this.FindControl<Avalonia.Controls.Button>("btnRemoveEventItem");
            btnCopyEventItem = this.FindControl<Avalonia.Controls.Button>("btnCopyEventItem");
            btnUpEventItem = this.FindControl<Avalonia.Controls.Button>("btnUpEventItem");
            btnDownEventItem = this.FindControl<Avalonia.Controls.Button>("btnDownEventItem");
            btnOptionEventItem = this.FindControl<Avalonia.Controls.Button>("btnOptionEventItem");
            eventListView = this.FindControl<Macro.View.EventListView>("eventListView");
        }

        private ProcessItem[] _processes;
        private Config _config;
        private CloseButtonWindow _closeButtonWindow;
        private readonly CoroutineHandler _coroutineHandler = new();
        private readonly DispatcherTimer _coroutineTimer = new() { Interval = TimeSpan.FromMilliseconds(33) };
        private readonly Stopwatch _coroutineClock = new();
        private bool _isShutdownHandled;
        private bool _shutdownApproved;
        private WebApiManager _webApiManager;
        private AdManager _adManager;
        private CacheDataManager _cacheDataManager;
        private bool _isEventItemMoveButtonPressed;
        private ImagePreviewWindow _imagePreviewWindow;
        private WebViewWindow _webViewWindow;
        private MacroExecutionController _macroExecutionController;
        private MacroExecutionController _runningExecutionController;
        private bool _isRunning;
        private bool _applicationUpdateInProgress;
        private bool _shutdownCompleted;

        public bool ShutdownInProgress
        {
            get
            {
                return _isShutdownHandled;
            }
        }
        public bool ShutdownCompleted
        {
            get
            {
                return _shutdownCompleted;
            }
        }
        public bool ApplicationUpdateInProgress
        {
            get
            {
                return _applicationUpdateInProgress;
            }
        }

        public bool TryBeginApplicationUpdate()
        {
            if (_isRunning == true)
            {
                return false;
            }
            if (_isShutdownHandled == true)
            {
                return false;
            }
            if (_applicationUpdateInProgress == true)
            {
                return false;
            }
            if (ApplicationManager.Instance.HasActiveInputOverlay == true)
            {
                return false;
            }
            foreach (Window window in OwnedWindows)
            {
                if (window is OptionDialog)
                {
                    if (window.IsVisible == true)
                    {
                        return false;
                    }
                }
            }
            _applicationUpdateInProgress = true;
            _isEventItemMoveButtonPressed = false;
            foreach (Button button in this.FindChildren<Button>())
            {
                button.IsEnabled = false;
            }
            comboProcess.IsEnabled = false;
            checkFix.IsEnabled = false;
            eventListView.IsEnabled = false;
            return true;
        }

        public void EndApplicationUpdate()
        {
            _applicationUpdateInProgress = false;
            if (_isShutdownHandled == false)
            {
                eventListView.IsEnabled = true;
                SetExecutionControls(_isRunning);
            }
        }
        public MainWindow()
        {
            InitializeComponent();
            Opened += MainWindow_Opened;
            Closed += MainWindow_Closed;
        }

        private async void MainWindow_Opened(object sender, EventArgs eventArgs)
        {
            _imagePreviewWindow = new ImagePreviewWindow();
            _webViewWindow = new WebViewWindow();
            _closeButtonWindow = new CloseButtonWindow(() => _webViewWindow.Hide());
            _config = ServiceResolver.GetService<Config>();
            _cacheDataManager = ServiceResolver.GetService<CacheDataManager>();
            _webApiManager = ServiceResolver.GetService<WebApiManager>();
            _adManager = ServiceResolver.GetService<AdManager>();
            _macroExecutionController = ServiceResolver.GetService<MacroExecutionController>();
            _macroExecutionController.InitializeController(_imagePreviewWindow.DrawImage, UpdateStatus);
            InitEvent();
            Refresh();
            LoadSaveFile(GetSaveFilePath());
            ApplicationManager.Instance.Init();
            _adManager.InitializeAdUrls();
            _coroutineTimer.Tick += UpdateCoroutines;
            _coroutineClock.Start();
            _coroutineTimer.Start();
            if (_config.OpenProcessPreview == true)
            {
                _imagePreviewWindow.Show(this);
            }
            try
            {
                bool isSponsor = await CheckSponsor();
                if (_isShutdownHandled == true)
                {
                    return;
                }
                if (isSponsor == false)
                {
                    await ShowAdAsync(true);
                }
                if (_isShutdownHandled == false)
                {
                    if (ServiceResolver.GetService<ApplicationUpdateRestartPresenter>().OpenSettingsAfterRestart == true)
                    {
                        UIManager.Instance.AddPopup<SettingView>();
                    }
                }
            }
            catch (Exception exception)
            {
                LogHelper.Error(exception);
            }
        }

        private void UpdateCoroutines(object sender, EventArgs eventArgs)
        {
            float elapsedSeconds = (float)_coroutineClock.Elapsed.TotalSeconds;
            _coroutineClock.Restart();
            _coroutineHandler.UpdateCoroutines(elapsedSeconds);
        }

        private async Task<bool> CheckSponsor()
        {
            CheckSponsorshipResponse response = await _webApiManager.Request<CheckSponsorshipResponse>(new CheckSponsorship { AccessKey = _config.AccessKey });
            if (response == null)
            {
                return false;
            }
            return response.IsSponsor;
        }

        protected override void OnClosing(WindowClosingEventArgs eventArgs)
        {
            if (_shutdownApproved == true)
            {
                base.OnClosing(eventArgs);
                return;
            }
            eventArgs.Cancel = true;
            if (_isShutdownHandled == true)
            {
                return;
            }
            _isShutdownHandled = true;
            _ = ShutdownAsync();
        }

        private async Task ShutdownAsync()
        {
            try
            {
                await UIManager.Instance.PrepareForShutdownAsync();
                MacroExecutionController executionController = _runningExecutionController;
                if (executionController == null)
                {
                    executionController = _macroExecutionController;
                }
                if (executionController != null)
                {
                    await executionController.StopAsync();
                }
                await ShowAdAsync(false);
            }
            catch (Exception exception)
            {
                LogHelper.Error(exception);
            }
            try
            {
                await Macro.Infrastructure.Controller.CoroutineRunner.StopAsync();
            }
            catch (Exception exception)
            {
                LogHelper.Error(exception);
            }
            _shutdownApproved = true;
            Close();
        }

        private void MainWindow_Closed(object sender, EventArgs eventArgs)
        {
            _coroutineTimer.Stop();
            _coroutineHandler.StopAll();
            NotifyHelper.ConfigChanged -= NotifyHelper_ConfigChanged;
            NotifyHelper.ScreenCaptureCompleted -= ScreenCaptureCompleted;
            NotifyHelper.EventTriggerSaved -= NotifyHelper_EventTriggerSaved;
            ApplicationManager.Instance.Dispose();
            _closeButtonWindow?.Close();
            _webViewWindow?.Close();
            _imagePreviewWindow?.Close();
            _shutdownCompleted = true;
        }

        private void InitEvent()
        {
            NotifyHelper.ConfigChanged += NotifyHelper_ConfigChanged;
            NotifyHelper.ScreenCaptureCompleted += ScreenCaptureCompleted;
            NotifyHelper.EventTriggerSaved += NotifyHelper_EventTriggerSaved;
            btnSetting.Click += BtnSetting_Click;
            btnGithub.Click += BtnGithub_Click;
            btnStop.Click += BtnStop_Click;
            btnStart.Click += BtnStart_Click;
            btnRefresh.Click += BtnRefresh_Click;
            btnAddEventItem.Click += BtnAddEventItem_Click;
            btnRemoveEventItem.Click += BtnRemoveEventItem_Click;
            btnCopyEventItem.Click += BtnCopyEventItem_Click;
            btnDownEventItem.AddHandler(PointerPressedEvent, BtnDownEventItem_PointerPressed, RoutingStrategies.Tunnel);
            btnDownEventItem.AddHandler(PointerReleasedEvent, MoveEventItem_PointerReleased, RoutingStrategies.Tunnel);
            btnUpEventItem.AddHandler(PointerPressedEvent, BtnUpEventItem_PointerPressed, RoutingStrategies.Tunnel);
            btnUpEventItem.AddHandler(PointerReleasedEvent, MoveEventItem_PointerReleased, RoutingStrategies.Tunnel);
            btnOptionEventItem.Click += BtnOptionEventItem_Click;
            checkFix.Click += CheckFix_Click;
            comboProcess.SelectionChanged += ComboProcess_SelectionChanged;
            eventListView.treeGridView.SelectionChanged += TreeGridView_SelectionChanged;
            KeyDown += MainWindow_KeyDown;
        }

        private void NotifyHelper_EventTriggerSaved(EventInfoEventArgs eventArgs)
        {
            Save();
        }

        private async void BtnOptionEventItem_Click(object sender, RoutedEventArgs eventArgs)
        {
            TreeGridViewItem selectedItem = ServiceResolver.GetService<SelectionStateController>().SelectTreeGridViewItem;
            if (selectedItem == null)
            {
                return;
            }
            OptionDialog optionDialog = new();
            optionDialog.BindItem(selectedItem.DataContext<EventInfoModel>());
            await optionDialog.ShowDialog(this);
        }

        private void MoveEventItem_PointerReleased(object sender, PointerReleasedEventArgs eventArgs)
        {
            _isEventItemMoveButtonPressed = false;
        }

        private void BtnUpEventItem_PointerPressed(object sender, PointerPressedEventArgs eventArgs)
        {
            MoveSelectedEventItem(-1, btnUpEventItem);
        }

        private void BtnDownEventItem_PointerPressed(object sender, PointerPressedEventArgs eventArgs)
        {
            MoveSelectedEventItem(1, btnDownEventItem);
        }

        private void MoveSelectedEventItem(int direction, Button button)
        {
            TreeGridViewItem selectedItem = ServiceResolver.GetService<SelectionStateController>().SelectTreeGridViewItem;
            if (selectedItem == null)
            {
                return;
            }
            EventListViewModel viewModel = eventListView.DataContext<EventListViewModel>();
            int currentIndex = viewModel.EventItems.IndexOf(selectedItem.DataContext<EventInfoModel>());
            if (currentIndex < 0)
            {
                return;
            }
            int nextIndex = currentIndex + direction;
            if (nextIndex < 0)
            {
                return;
            }
            if (nextIndex >= viewModel.EventItems.Count)
            {
                return;
            }
            _isEventItemMoveButtonPressed = true;
            viewModel.EventItems.Move(currentIndex, nextIndex);
            _coroutineHandler.Start(2, RepeatMoveEventItemCoroutine(button, selectedItem));
        }

        private IEnumerator RepeatMoveEventItemCoroutine(Button button, TreeGridViewItem selectedItem)
        {
            int direction = 1;
            if (button == btnUpEventItem)
            {
                direction = -1;
            }
            EventListViewModel viewModel = eventListView.DataContext<EventListViewModel>();
            EventInfoModel eventInfo = selectedItem.DataContext<EventInfoModel>();
            while (_isEventItemMoveButtonPressed == true)
            {
                int currentIndex = viewModel.EventItems.IndexOf(eventInfo);
                if (currentIndex < 0)
                {
                    yield break;
                }
                int nextIndex = currentIndex + direction;
                if (nextIndex < 0)
                {
                    yield break;
                }
                if (nextIndex >= viewModel.EventItems.Count)
                {
                    yield break;
                }
                viewModel.EventItems.Move(currentIndex, nextIndex);
                yield return 0.3F;
            }
        }

        private void TreeGridView_SelectionChanged(object sender, SelectionChangedEventArgs eventArgs)
        {
            SelectionStateController selection = ServiceResolver.GetService<SelectionStateController>();
            selection.SelectTreeGridViewItem = eventListView.treeGridView.GetSelectItemFromObject<TreeGridViewItem>(eventListView.treeGridView.SelectedItem);
            if (selection.SelectTreeGridViewItem != null)
            {
                _imagePreviewWindow.DrawImage(selection.SelectTreeGridViewItem.DataContext<EventInfoModel>().Image);
            }
            RefreshEventItemButton();
        }

        private void RefreshEventItemButton()
        {
            if (_applicationUpdateInProgress == true)
            {
                return;
            }
            bool hasSelection = ServiceResolver.GetService<SelectionStateController>().SelectTreeGridViewItem != null;
            btnAddEventItem.IsEnabled = true;
            btnRemoveEventItem.IsEnabled = hasSelection;
            btnCopyEventItem.IsEnabled = hasSelection;
            btnUpEventItem.IsEnabled = hasSelection;
            btnDownEventItem.IsEnabled = hasSelection;
            btnOptionEventItem.IsEnabled = hasSelection;
        }

        private ProcessInfo GetSelectedProcessInfo()
        {
            if (comboProcess.SelectedItem is ProcessItem processItem == false)
            {
                return null;
            }
            IntRect rectangle = new();
            NativeHelper.GetWindowRect(processItem.Process.MainWindowHandle, ref rectangle);
            return new ProcessInfo { ProcessName = processItem.Process.ProcessName, Position = rectangle };
        }

        private async void ScreenCaptureCompleted(CaptureCompletedEventArgs eventArgs)
        {
            ApplicationManager.Instance.CloseCaptureView();
            Bitmap capture = eventArgs.CaptureImage;
            if (capture == null)
            {
                return;
            }
            ProcessInfo processInfo = GetSelectedProcessInfo();
            if (processInfo == null)
            {
                await ApplicationManager.ShowMessageDialog(TemplateContainer<LabelTemplate>.Find(1062).GetString(), TemplateContainer<MessageTemplate>.Find(1017).GetString());
                return;
            }
            EventInfoModel eventInfo = new() { Image = new Bitmap(capture, capture.Width, capture.Height), MonitorInfo = eventArgs.MonitorInfo, ProcessInfo = processInfo };
            eventListView.DataContext<EventListViewModel>().EventItems.Add(eventInfo);
            _cacheDataManager.MakeIndexTriggerModel(eventInfo);
            Save();
        }

        private void BtnCopyEventItem_Click(object sender, RoutedEventArgs eventArgs)
        {
            TreeGridViewItem selectedItem = ServiceResolver.GetService<SelectionStateController>().SelectTreeGridViewItem;
            if (selectedItem == null)
            {
                return;
            }
            EventInfoModel copyItem = new(selectedItem.DataContext<EventInfoModel>());
            _cacheDataManager.MakeIndexTriggerModel(copyItem);
            eventListView.DataContext<EventListViewModel>().EventItems.Add(copyItem);
            Save();
        }

        private void BtnRemoveEventItem_Click(object sender, RoutedEventArgs eventArgs)
        {
            SelectionStateController selection = ServiceResolver.GetService<SelectionStateController>();
            TreeGridViewItem selectedItem = selection.SelectTreeGridViewItem;
            if (selectedItem == null)
            {
                return;
            }
            EventInfoModel eventInfo = selectedItem.DataContext<EventInfoModel>();
            if (selectedItem.ParentItem == null)
            {
                eventListView.DataContext<EventListViewModel>().EventItems.Remove(eventInfo);
            }
            else
            {
                selectedItem.ParentItem.DataContext<EventInfoModel>().SubEventItems.Remove(eventInfo);
            }
            selection.UnselectTreeGridViewItem();
            eventListView.treeGridView.SelectedItem = null;
            _imagePreviewWindow.ClearImage();
            RefreshEventItemButton();
            Save();
        }

        private async void BtnAddEventItem_Click(object sender, RoutedEventArgs eventArgs)
        {
            if (comboProcess.SelectedItem == null)
            {
                await ApplicationManager.ShowMessageDialog(TemplateContainer<LabelTemplate>.Find(1062).GetString(), TemplateContainer<MessageTemplate>.Find(1017).GetString());
                return;
            }
            ApplicationManager.Instance.ShowCaptureImageViews();
        }

        private void BtnRefresh_Click(object sender, RoutedEventArgs eventArgs)
        {
            Refresh();
        }

        private async void BtnStart_Click(object sender, RoutedEventArgs eventArgs)
        {
            if (_applicationUpdateInProgress == true)
            {
                return;
            }
            if (_isRunning == true)
            {
                return;
            }
            SelectionStateController selection = ServiceResolver.GetService<SelectionStateController>();
            selection.UnselectTreeGridViewItem();
            eventListView.treeGridView.SelectedItem = null;
            Process process = null;
            if (selection.SelectProcessItem != null)
            {
                process = selection.SelectProcessItem.Process;
            }
            ArrayQueue<EventInfoModel> eventInfos = new();
            eventInfos.AddRange(eventListView.DataContext<EventListViewModel>().EventItems.Where(item => item.IsChecked == true));
            MacroExecutionController executionController = _macroExecutionController;
            executionController.Start(eventInfos, process);
            _runningExecutionController = executionController;
            _isRunning = true;
            SetExecutionControls(true);
            try
            {
                await executionController.Completion;
            }
            catch (Exception exception)
            {
                LogHelper.Error(exception);
            }
            finally
            {
                if (_runningExecutionController == executionController)
                {
                    _runningExecutionController = null;
                    _isRunning = false;
                    SetExecutionControls(false);
                }
            }
        }

        private async void BtnStop_Click(object sender, RoutedEventArgs eventArgs)
        {
            if (_isRunning == false)
            {
                return;
            }
            MacroExecutionController executionController = _runningExecutionController;
            ApplicationManager.ShowProgressbar();
            btnStop.IsEnabled = false;
            try
            {
                await executionController.StopAsync();
            }
            catch (Exception exception)
            {
                LogHelper.Error(exception);
            }
            finally
            {
                if (_isShutdownHandled == false)
                {
                    ApplicationManager.HideProgressbar();
                    if (_applicationUpdateInProgress == false)
                    {
                        btnStop.IsEnabled = true;
                    }
                }
            }
        }

        private void SetExecutionControls(bool isRunning)
        {
            if (_isShutdownHandled == true)
            {
                return;
            }
            if (_applicationUpdateInProgress == true)
            {
                return;
            }
            foreach (Button button in this.FindChildren<Button>())
            {
                if (button == btnStart)
                {
                    continue;
                }
                if (button == btnStop)
                {
                    continue;
                }
                button.IsEnabled = isRunning == false;
            }
            comboProcess.IsEnabled = isRunning == false;
            checkFix.IsEnabled = isRunning == false;
            btnStop.IsEnabled = true;
            btnStop.IsVisible = isRunning;
            btnStart.IsVisible = isRunning == false;
            if (isRunning == false)
            {
                RefreshEventItemButton();
            }
        }

        private void UpdateStatus(EventInfoModel eventInfo, string status)
        {
            Dispatcher.UIThread.Post(() =>
            {
                if (_isShutdownHandled == false)
                {
                    eventInfo.Status = status;
                }
            });
        }

        private void CheckFix_Click(object sender, RoutedEventArgs eventArgs)
        {
            SelectionStateController selection = ServiceResolver.GetService<SelectionStateController>();
            selection.SelectProcessItem = null;
            if (checkFix.IsChecked == true)
            {
                selection.SelectProcessItem = comboProcess.SelectedItem as ProcessItem;
            }
        }

        private void BtnGithub_Click(object sender, RoutedEventArgs eventArgs)
        {
            Process.Start(new ProcessStartInfo(ConstHelper.HelpUrl) { UseShellExecute = true });
        }

        private void BtnSetting_Click(object sender, RoutedEventArgs eventArgs)
        {
            UIManager.Instance.AddPopup<SettingView>();
        }

        private void MainWindow_KeyDown(object sender, KeyEventArgs eventArgs)
        {
            if (_applicationUpdateInProgress == true)
            {
                return;
            }
            if (eventArgs.Key == Key.Escape)
            {
                ServiceResolver.GetService<SelectionStateController>().UnselectTreeGridViewItem();
                eventListView.treeGridView.SelectedItem = null;
                _imagePreviewWindow.ClearImage();
                RefreshEventItemButton();
                BtnStop_Click(btnStop, null);
            }
        }

        private string GetSaveFilePath()
        {
            return Path.Combine(GetSaveDirectoryPath(), ConstHelper.DefaultSaveFileName);
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
            Directory.CreateDirectory(GetSaveDirectoryPath());
            JsonHelper.ConfigureImageDirectory(Path.Combine(GetSaveDirectoryPath(), "img"));
            _processes = Process.GetProcesses().Where(process => process.MainWindowHandle != IntPtr.Zero).Select(process => new ProcessItem(process)).OrderBy(process => process.ProcessName).ToArray();
            comboProcess.ItemsSource = _processes;
        }

        private void Save()
        {
            ServiceResolver.GetService<FileService>().SaveAsJson(GetSaveFilePath(), eventListView.DataContext<EventListViewModel>().EventItems.ToList());
        }

        private void ComboProcess_SelectionChanged(object sender, SelectionChangedEventArgs eventArgs)
        {
            CheckFix_Click(checkFix, null);
        }

        public void LoadSaveFile(string path)
        {
            FileService fileService = ServiceResolver.GetService<FileService>();
            List<EventInfoModel> eventInfos;
            if (fileService.LooksLikeJson(path) == true)
            {
                eventInfos = fileService.LoadSaveAsJson(path);
                if (eventInfos == null)
                {
                    eventInfos = new List<EventInfoModel>();
                }
            }
            else
            {
                eventInfos = EventModelConverter.ToEventInfoList(fileService.Load<EventTriggerModel>(path));
            }
            _cacheDataManager.InitDatas(eventInfos);
            EventListViewModel viewModel = eventListView.DataContext<EventListViewModel>();
            viewModel.EventItems.Clear();
            foreach (EventInfoModel eventInfo in eventInfos)
            {
                viewModel.EventItems.Add(eventInfo);
            }
        }

        private void NotifyHelper_ConfigChanged(ConfigEventArgs eventArgs)
        {
            ApplicationManager.ShowProgressbar();
            try
            {
                _config = eventArgs.Config;
                Refresh();
                LoadSaveFile(GetSaveFilePath());
                _macroExecutionController = ServiceResolver.GetService<MacroExecutionController>();
                _macroExecutionController.InitializeController(_imagePreviewWindow.DrawImage, UpdateStatus);
                if (_config.OpenProcessPreview == false)
                {
                    _imagePreviewWindow.Hide();
                }
                else if (_imagePreviewWindow.IsVisible == false)
                {
                    _imagePreviewWindow.Show(this);
                }
            }
            finally
            {
                ApplicationManager.HideProgressbar();
            }
        }

        private async Task ShowAdAsync(bool isCloseButtonShow)
        {
#if DEBUG
            return;
#else
            if (_webViewWindow.IsVisible == false)
            {
                _webViewWindow.Show(this);
            }
            string address = await _adManager.GetRandomAdUrl();
            if (isCloseButtonShow == true)
            {
                if (_isShutdownHandled == true)
                {
                    return;
                }
            }
            await _webViewWindow.LoadUrlAsync(address);
            await Task.Delay(TimeSpan.FromSeconds(3.5));
            if (isCloseButtonShow == true)
            {
                if (_isShutdownHandled == false)
                {
                    _closeButtonWindow.SetOwner(_webViewWindow);
                    _closeButtonWindow.Show(_webViewWindow);
                }
            }
#endif
        }
    }
}

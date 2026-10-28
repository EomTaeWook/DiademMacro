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
        internal Macro.View.ImagePreviewView imagePreviewView;

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
            imagePreviewView = this.FindControl<Macro.View.ImagePreviewView>("imagePreviewView");
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
        private bool _eventItemOrderChanged;
        private WebViewWindow _webViewWindow;
        private MacroExecutionController _macroExecutionController;
        private MacroExecutionController _runningExecutionController;
        private Task _executionCompletionTask = Task.CompletedTask;
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
            _webViewWindow = new WebViewWindow();
            _closeButtonWindow = new CloseButtonWindow(() => _webViewWindow.Hide());
            _config = ServiceResolver.GetService<Config>();
            _cacheDataManager = ServiceResolver.GetService<CacheDataManager>();
            _webApiManager = ServiceResolver.GetService<WebApiManager>();
            _adManager = ServiceResolver.GetService<AdManager>();
            _macroExecutionController = ServiceResolver.GetService<MacroExecutionController>();
            _macroExecutionController.InitializeController(imagePreviewView.DrawImage, UpdateStatus);
            InitEvent();
            Refresh();
            LoadSaveFile(GetSaveFilePath());
            ApplicationManager.Instance.Init();
            _coroutineTimer.Tick += UpdateCoroutines;
            _coroutineClock.Start();
            _coroutineTimer.Start();
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
            _isEventItemMoveButtonPressed = false;
            IsEnabled = false;
            _ = ShutdownAsync();
        }

        private async Task ShutdownAsync()
        {
            Task buttonActionCompletionTask = UIManager.Instance.StopButtonActionsAsync();
            try
            {
                await UIManager.Instance.PrepareForShutdownAsync();
                await buttonActionCompletionTask;
                MacroExecutionController executionController = _runningExecutionController;
                if (executionController == null)
                {
                    executionController = _macroExecutionController;
                }
                if (executionController != null)
                {
                    await executionController.StopAsync();
                }
                await _executionCompletionTask;
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
            UIManager.Instance.ButtonActionStateChanged -= UpdateQueuedInputState;
            _coroutineTimer.Stop();
            _coroutineHandler.StopAll();
            NotifyHelper.ConfigChanged -= NotifyHelper_ConfigChanged;
            NotifyHelper.ScreenCaptureCompleted -= ScreenCaptureCompleted;
            NotifyHelper.EventTriggerSaved -= NotifyHelper_EventTriggerSaved;
            ApplicationManager.Instance.Dispose();
            _closeButtonWindow?.Close();
            _webViewWindow?.Close();
            imagePreviewView.Dispose();
            DisposeEventItems();
            DisposeProcessItems(_processes);
            _processes = null;
            _shutdownCompleted = true;
        }

        private void InitEvent()
        {
            UIManager.Instance.ButtonActionStateChanged += UpdateQueuedInputState;
            NotifyHelper.ConfigChanged += NotifyHelper_ConfigChanged;
            NotifyHelper.ScreenCaptureCompleted += ScreenCaptureCompleted;
            NotifyHelper.EventTriggerSaved += NotifyHelper_EventTriggerSaved;
            btnSetting.Click += (sender, eventArgs) => UIManager.Instance.EnqueueButtonAction(() => BtnSetting_Click(sender, eventArgs));
            btnGithub.Click += (sender, eventArgs) => UIManager.Instance.EnqueueButtonAction(() => BtnGithub_Click(sender, eventArgs));
            btnStop.Click += (sender, eventArgs) => RequestStop();
            btnStart.Click += (sender, eventArgs) => UIManager.Instance.EnqueueButtonAction(() => BtnStart_Click(sender, eventArgs));
            btnRefresh.Click += (sender, eventArgs) => UIManager.Instance.EnqueueButtonAction(() => BtnRefresh_Click(sender, eventArgs));
            btnAddEventItem.Click += (sender, eventArgs) => UIManager.Instance.EnqueueButtonAction(() => BtnAddEventItemAsync(sender, eventArgs));
            btnRemoveEventItem.Click += (sender, eventArgs) => UIManager.Instance.EnqueueButtonAction(() => BtnRemoveEventItemAsync(sender, eventArgs));
            btnCopyEventItem.Click += (sender, eventArgs) => UIManager.Instance.EnqueueButtonAction(() => BtnCopyEventItemAsync(sender, eventArgs));
            btnDownEventItem.AddHandler(PointerPressedEvent, BtnDownEventItem_PointerPressed, RoutingStrategies.Tunnel);
            btnDownEventItem.AddHandler(PointerReleasedEvent, MoveEventItem_PointerReleased, RoutingStrategies.Tunnel);
            btnUpEventItem.AddHandler(PointerPressedEvent, BtnUpEventItem_PointerPressed, RoutingStrategies.Tunnel);
            btnUpEventItem.AddHandler(PointerReleasedEvent, MoveEventItem_PointerReleased, RoutingStrategies.Tunnel);
            btnOptionEventItem.Click += (sender, eventArgs) => UIManager.Instance.EnqueueButtonAction(() => BtnOptionEventItem_Click(sender, eventArgs));
            checkFix.Click += (sender, eventArgs) => UIManager.Instance.EnqueueButtonAction(() => CheckFix_Click(sender, eventArgs));
            comboProcess.SelectionChanged += ComboProcess_SelectionChanged;
            eventListView.treeGridView.SelectionChanged += TreeGridView_SelectionChanged;
            KeyDown += MainWindow_KeyDown;
        }

        private void NotifyHelper_EventTriggerSaved(EventInfoEventArgs eventArgs)
        {
            UIManager.Instance.EnqueueButtonAction(SaveAsync);
        }

        private void BtnOptionEventItem_Click(object sender, RoutedEventArgs eventArgs)
        {
            if (CanEditEvents() == false)
            {
                return;
            }
            TreeGridViewItem selectedItem = ServiceResolver.GetService<SelectionStateController>().SelectTreeGridViewItem;
            if (selectedItem == null)
            {
                return;
            }
            OptionDialog optionDialog = new();
            optionDialog.BindItem(selectedItem.DataContext<EventInfoModel>());
            if (UIManager.Instance.TrackPopup(optionDialog) == false)
            {
                return;
            }
            _ = optionDialog.ShowDialog(this);
        }

        private void MoveEventItem_PointerReleased(object sender, PointerReleasedEventArgs eventArgs)
        {
            _isEventItemMoveButtonPressed = false;
            if (_eventItemOrderChanged == false)
            {
                return;
            }
            _eventItemOrderChanged = false;
            UIManager.Instance.EnqueueButtonAction(SaveAsync);
        }

        private void BtnUpEventItem_PointerPressed(object sender, PointerPressedEventArgs eventArgs)
        {
            _isEventItemMoveButtonPressed = true;
            UIManager.Instance.EnqueueButtonAction(() => MoveSelectedEventItem(-1, btnUpEventItem));
        }

        private void BtnDownEventItem_PointerPressed(object sender, PointerPressedEventArgs eventArgs)
        {
            _isEventItemMoveButtonPressed = true;
            UIManager.Instance.EnqueueButtonAction(() => MoveSelectedEventItem(1, btnDownEventItem));
        }

        private void MoveSelectedEventItem(int direction, Button button)
        {
            if (CanEditEvents() == false)
            {
                return;
            }
            TreeGridViewItem selectedItem = ServiceResolver.GetService<SelectionStateController>().SelectTreeGridViewItem;
            if (selectedItem == null)
            {
                return;
            }
            EventListViewModel viewModel = eventListView.DataContext<EventListViewModel>();
            if (MoveEventItem(viewModel, selectedItem.DataContext<EventInfoModel>(), direction) == false)
            {
                return;
            }
            if (_isEventItemMoveButtonPressed == false)
            {
                _eventItemOrderChanged = false;
                UIManager.Instance.EnqueueButtonAction(SaveAsync);
                return;
            }
            _coroutineHandler.Start(2, RepeatMoveEventItemCoroutine(button, selectedItem));
        }

        private bool MoveEventItem(EventListViewModel eventListViewModel, EventInfoModel eventInfoModel, int direction)
        {
            if (CanEditEvents() == false)
            {
                return false;
            }
            int currentIndex = eventListViewModel.EventItems.IndexOf(eventInfoModel);
            if (currentIndex < 0)
            {
                return false;
            }
            int nextIndex = currentIndex + direction;
            if (nextIndex < 0)
            {
                return false;
            }
            if (nextIndex >= eventListViewModel.EventItems.Count)
            {
                return false;
            }
            eventListViewModel.EventItems.Move(currentIndex, nextIndex);
            _eventItemOrderChanged = true;
            return true;
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
                if (CanEditEvents() == false)
                {
                    yield break;
                }
                UIManager.Instance.EnqueueButtonAction(() =>
                {
                    if (_isEventItemMoveButtonPressed == false)
                    {
                        return;
                    }
                    if (MoveEventItem(viewModel, eventInfo, direction) == false)
                    {
                        _isEventItemMoveButtonPressed = false;
                    }
                });
                yield return 0.3F;
            }
        }

        private void TreeGridView_SelectionChanged(object sender, SelectionChangedEventArgs eventArgs)
        {
            SelectionStateController selection = ServiceResolver.GetService<SelectionStateController>();
            selection.SelectTreeGridViewItem = eventListView.treeGridView.GetSelectItemFromObject<TreeGridViewItem>(eventListView.treeGridView.SelectedItem);
            if (selection.SelectTreeGridViewItem != null)
            {
                imagePreviewView.DrawImage(selection.SelectTreeGridViewItem.DataContext<EventInfoModel>().Image);
            }
            RefreshEventItemButton();
        }

        private bool CanEditEvents()
        {
            if (_isShutdownHandled == true)
            {
                return false;
            }
            if (_isRunning == true)
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
            return true;
        }

        private void RefreshEventItemButton()
        {
            btnAddEventItem.IsEnabled = false;
            btnRemoveEventItem.IsEnabled = false;
            btnCopyEventItem.IsEnabled = false;
            btnUpEventItem.IsEnabled = false;
            btnDownEventItem.IsEnabled = false;
            btnOptionEventItem.IsEnabled = false;
            if (CanEditEvents() == false)
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

        private void ScreenCaptureCompleted(CaptureCompletedEventArgs eventArgs)
        {
            ApplicationManager.Instance.CloseCaptureView();
            if (UIManager.Instance.EnqueueButtonAction(() => ProcessScreenCaptureCompletedAsync(eventArgs)) == false)
            {
                eventArgs.CaptureImage?.Dispose();
            }
        }

        private async Task ProcessScreenCaptureCompletedAsync(CaptureCompletedEventArgs eventArgs)
        {
            using Bitmap capture = eventArgs.CaptureImage;
            if (CanEditEvents() == false)
            {
                return;
            }
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
            await SaveAsync();
        }

        private async Task BtnCopyEventItemAsync(object sender, RoutedEventArgs eventArgs)
        {
            if (CanEditEvents() == false)
            {
                return;
            }
            TreeGridViewItem selectedItem = ServiceResolver.GetService<SelectionStateController>().SelectTreeGridViewItem;
            if (selectedItem == null)
            {
                return;
            }
            EventInfoModel copyItem = new(selectedItem.DataContext<EventInfoModel>());
            _cacheDataManager.MakeIndexTriggerModel(copyItem);
            eventListView.DataContext<EventListViewModel>().EventItems.Add(copyItem);
            await SaveAsync();
        }

        private async Task BtnRemoveEventItemAsync(object sender, RoutedEventArgs eventArgs)
        {
            if (CanEditEvents() == false)
            {
                return;
            }
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
            _cacheDataManager.RemoveIndexEventModel(eventInfo);
            selection.UnselectTreeGridViewItem();
            eventListView.treeGridView.SelectedItem = null;
            imagePreviewView.ClearImage();
            eventInfo.Dispose();
            RefreshEventItemButton();
            await SaveAsync();
        }

        private async Task BtnAddEventItemAsync(object sender, RoutedEventArgs eventArgs)
        {
            if (CanEditEvents() == false)
            {
                return;
            }
            if (comboProcess.SelectedItem == null)
            {
                await ApplicationManager.ShowMessageDialog(TemplateContainer<LabelTemplate>.Find(1062).GetString(), TemplateContainer<MessageTemplate>.Find(1017).GetString());
                return;
            }
            ApplicationManager.Instance.ShowCaptureImageViews();
        }

        private void BtnRefresh_Click(object sender, RoutedEventArgs eventArgs)
        {
            if (CanEditEvents() == false)
            {
                return;
            }
            Refresh();
        }

        private void BtnStart_Click(object sender, RoutedEventArgs eventArgs)
        {
            if (CanEditEvents() == false)
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
            _isEventItemMoveButtonPressed = false;
            SetExecutionControls(true);
            _executionCompletionTask = ObserveExecutionAsync(executionController);
        }

        private async Task ObserveExecutionAsync(MacroExecutionController executionController)
        {
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
                try
                {
                    await executionController.StopAsync();
                }
                catch (Exception exception)
                {
                    LogHelper.Error(exception);
                }
                if (_runningExecutionController == executionController)
                {
                    _runningExecutionController = null;
                    _isRunning = false;
                    SetExecutionControls(false);
                }
            }
        }

        private void RequestStop()
        {
            MacroExecutionController executionController = _runningExecutionController;
            if (executionController == null)
            {
                return;
            }
            executionController.Stop();
            UIManager.Instance.EnqueueButtonAction(() => StopMacroAsync(executionController));
        }

        private async Task StopMacroAsync(MacroExecutionController executionController)
        {
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
            UpdateQueuedInputState();
            btnStop.IsEnabled = true;
            btnStop.IsVisible = isRunning;
            btnStart.IsVisible = isRunning == false;
            if (isRunning == false)
            {
                RefreshEventItemButton();
            }
        }

        private void UpdateQueuedInputState()
        {
            bool canUseInputs = _isRunning == false;
            if (_isShutdownHandled == true)
            {
                canUseInputs = false;
            }
            if (_applicationUpdateInProgress == true)
            {
                canUseInputs = false;
            }
            if (UIManager.Instance.IsProcessingButtonActions == true)
            {
                canUseInputs = false;
            }
            comboProcess.IsEnabled = canUseInputs;
            checkFix.IsEnabled = canUseInputs;
            eventListView.IsEnabled = canUseInputs;
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
            if (_isShutdownHandled == true)
            {
                return;
            }
            Process.Start(new ProcessStartInfo(ConstHelper.HelpUrl) { UseShellExecute = true });
        }

        private void BtnSetting_Click(object sender, RoutedEventArgs eventArgs)
        {
            if (CanEditEvents() == false)
            {
                return;
            }
            UIManager.Instance.AddPopup<SettingView>();
        }

        private void MainWindow_KeyDown(object sender, KeyEventArgs eventArgs)
        {
            if (_isShutdownHandled == true)
            {
                return;
            }
            if (_applicationUpdateInProgress == true)
            {
                return;
            }
            if (eventArgs.Key == Key.Escape)
            {
                ServiceResolver.GetService<SelectionStateController>().UnselectTreeGridViewItem();
                eventListView.treeGridView.SelectedItem = null;
                imagePreviewView.ClearImage();
                RefreshEventItemButton();
                RequestStop();
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
            List<ProcessItem> currentProcesses = new List<ProcessItem>();
            foreach (Process process in Process.GetProcesses())
            {
                try
                {
                    if (process.MainWindowHandle == IntPtr.Zero)
                    {
                        process.Dispose();
                        continue;
                    }
                    currentProcesses.Add(new ProcessItem(process));
                }
                catch (Exception exception)
                {
                    process.Dispose();
                    LogHelper.Error(exception);
                }
            }
            ProcessItem[] previousProcesses = _processes;
            ServiceResolver.GetService<SelectionStateController>().SelectProcessItem = null;
            comboProcess.SelectedItem = null;
            _processes = currentProcesses.OrderBy(process => process.ProcessName).ToArray();
            comboProcess.ItemsSource = _processes;
            DisposeProcessItems(previousProcesses);
        }

        private async Task SaveAsync()
        {
            bool saved = ServiceResolver.GetService<FileService>().SaveAsJson(GetSaveFilePath(), eventListView.DataContext<EventListViewModel>().EventItems.ToList());
            if (saved == true)
            {
                return;
            }
            if (_isShutdownHandled == true)
            {
                return;
            }
            await ApplicationManager.ShowMessageDialog(TemplateContainer<LabelTemplate>.Find(1062).GetString(), TemplateContainer<MessageTemplate>.Find(1015).GetString());
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
            List<EventInfoModel> previousEventInfos = viewModel.EventItems.ToList();
            SelectionStateController selection = ServiceResolver.GetService<SelectionStateController>();
            selection.UnselectTreeGridViewItem();
            eventListView.treeGridView.SelectedItem = null;
            imagePreviewView.ClearImage();
            viewModel.EventItems.Clear();
            foreach (EventInfoModel eventInfo in eventInfos)
            {
                viewModel.EventItems.Add(eventInfo);
            }
            foreach (EventInfoModel previousEventInfo in previousEventInfos)
            {
                previousEventInfo.Dispose();
            }
        }

        private void DisposeEventItems()
        {
            EventListViewModel viewModel = eventListView.DataContext<EventListViewModel>();
            foreach (EventInfoModel eventInfoModel in viewModel.EventItems)
            {
                eventInfoModel.Dispose();
            }
            viewModel.EventItems.Clear();
        }

        private static void DisposeProcessItems(ProcessItem[] processItems)
        {
            if (processItems == null)
            {
                return;
            }
            foreach (ProcessItem processItem in processItems)
            {
                processItem.Dispose();
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
                _macroExecutionController.InitializeController(imagePreviewView.DrawImage, UpdateStatus);
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
            string address = await _adManager.GetAdUrlAsync();
            if (string.IsNullOrEmpty(address) == true)
            {
                return;
            }
            if (isCloseButtonShow == true)
            {
                if (_isShutdownHandled == true)
                {
                    return;
                }
            }
            if (_webViewWindow.IsVisible == false)
            {
                _webViewWindow.Show(this);
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

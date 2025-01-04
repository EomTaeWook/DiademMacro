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
using System.Windows.Data;
using System.Windows.Threading;
using TemplateContainers;
using Utils;
using Utils.Extensions;
using Utils.Infrastructure;
using Label = System.Windows.Controls.Label;
using Rect = Utils.Infrastructure.Rect;

namespace Macro
{
    /// <summary>
    /// MainWindow.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class MainWindow : MetroWindow
    {
        private KeyValuePair<string, Process>[] _processes;
        private KeyValuePair<string, Process>? _fixProcess;
        private Config _config;
        private ContentController _contentController;
        private CloseButtonWindow _closeButtonWindow;
        private CoroutineHandler _coroutineHandler = new CoroutineHandler();
        private bool _isShutdownHandled;
        public MainWindow()
        {
            InitializeComponent();
            Loaded += MainWindow_Loaded;
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            _contentController = ServiceDispatcher.Resolve<ContentController>();
            _config = ServiceDispatcher.Resolve<Config>();
            InitEvent();
            Init();
            _closeButtonWindow = new CloseButtonWindow(this, () =>
            {
                AdOverlay.Visibility = Visibility.Collapsed;
            });

            if (VersionCheck() == false)
            {
                _coroutineHandler.Start(ShowAd(true));
            }
            ApplicationManager.Instance.Init();
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
            btnRefresh.Click += Button_Click;
            btnStart.Click += Button_Click;
            btnStop.Click += Button_Click;
            btnSetting.Click += Button_Click;
            btnGithub.Click += Button_Click;
            btnMoveProcessLocation.Click += Button_Click;
            btnRestoreMoveProcessLocation.Click += Button_Click;

            checkFix.Checked += CheckFix_Checked;
            checkFix.Unchecked += CheckFix_Checked;
            comboProcess.SelectionChanged += ComboProcess_SelectionChanged;

            KeyDown += MainWindow_KeyDown;

            NotifyHelper.ConfigChanged += NotifyHelper_ConfigChanged;
            NotifyHelper.TreeItemOrderChanged += NotifyHelper_TreeItemOrderChanged;
            NotifyHelper.SelectTreeViewChanged += NotifyHelper_SelectTreeViewChanged;
            NotifyHelper.EventTriggerOrderChanged += NotifyHelper_EventTriggerOrderChanged;
            NotifyHelper.SaveEventTriggerModel += NotifyHelper_SaveEventTriggerModel;
            NotifyHelper.DeleteEventTriggerModel += NotifyHelper_DeleteEventTriggerModel;
            NotifyHelper.UpdatedTime += NotifyHelper_UpdatedTime;
        }

        private void NotifyHelper_UpdatedTime(UpdatedTimeArgs obj)
        {
            _coroutineHandler.UpdateCoroutines(obj.DeltaTime);
        }

        private void MainWindow_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == System.Windows.Input.Key.Escape)
            {
                Button_Click(btnStop, null);
            }
        }

        private void Init()
        {
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
            if (string.IsNullOrEmpty(_config.SavePath) == true)
            {
                return $"{ConstHelper.DefaultSavePath}{ConstHelper.DefaultSaveFileName}";
            }
            else
            {
                return $"{_config.SavePath}\\{ConstHelper.DefaultSaveFileName}";
            }
        }

        private void Refresh()
        {
            FileInfo fileInfo = new FileInfo(GetSaveFilePath());
            if (fileInfo.Directory.Exists == false)
            {
                fileInfo.Directory.Create();
            }

            _processes = Process.GetProcesses().Where(r => r.MainWindowHandle != IntPtr.Zero)
                                                .Select(r => new KeyValuePair<string, Process>($"{r.ProcessName}:{r.Id}", r))
                                                .OrderBy(r => r.Key).ToArray();

            comboProcess.ItemsSource = _processes;
            comboProcess.DisplayMemberPath = "Key";
            comboProcess.SelectedValuePath = "Value";

            var labels = ObjectExtensions.FindChildren<Label>(this);
            foreach (var label in labels)
            {
                BindingOperations.GetBindingExpressionBase(label, ContentProperty).UpdateTarget();
            }
            var buttons = ObjectExtensions.FindChildren<Button>(this);
            foreach (var button in buttons)
            {
                if (button.Equals(btnSetting) ||
                    button.Content == null ||
                    !(button.Content is string) ||
                    button.Content.Equals("Close"))
                {
                    continue;
                }

                BindingOperations.GetBindingExpressionBase(button, ContentProperty).UpdateTarget();
            }
            var checkBoxs = ObjectExtensions.FindChildren<CheckBox>(this);
            foreach (var checkBox in checkBoxs)
            {
                if (checkBox.Content == null || !(checkBox.Content is string))
                {
                    continue;
                }

                BindingOperations.GetBindingExpressionBase(checkBox, ContentProperty).UpdateTarget();
            }

            BindingOperations.GetBindingExpressionBase(this, TitleProperty).UpdateTarget();

            Clear();

            _contentController.SetContentView(contentView);
        }
        private void SettingProcessMonitorInfo(EventTriggerModel model, Process process)
        {
            if (process == null)
            {
                model.ProcessInfo = new ProcessInfo()
                {
                    ProcessName = string.Empty
                };
                return;
            }

            var rect = new Rect();
            NativeHelper.GetWindowRect(process.MainWindowHandle, ref rect);
            model.ProcessInfo = new ProcessInfo()
            {
                ProcessName = process.ProcessName,
                Position = rect
            };

            if (model.EventType != EventType.Mouse)
            {
                if (model.Image == null)
                {
                    return;
                }
                foreach (var monitor in DisplayHelper.MonitorInfo())
                {
                    if (monitor.Rect.IsContain(rect))
                    {
                        if (model.EventType != EventType.Mouse)
                        {
                            if (model.MonitorInfo != null)
                            {
                                model.Image = model.Image.Resize((int)(model.Image.Width * (monitor.Dpi.X * 1.0F / model.MonitorInfo.Dpi.X)), (int)(model.Image.Height * (monitor.Dpi.Y * 1.0F / model.MonitorInfo.Dpi.Y)));
                            }
                            model.MonitorInfo = monitor;

                        }
                        if (model.RoiData.IsExists() == true)
                        {
                            model.RoiData.RoiRect.Left -= rect.Left - monitor.Rect.Left;
                            model.RoiData.RoiRect.Top -= rect.Top - monitor.Rect.Top;
                            model.RoiData.RoiRect.Right -= rect.Left - monitor.Rect.Left;
                            model.RoiData.RoiRect.Bottom -= rect.Top - monitor.Rect.Top;
                        }
                        break;
                    }
                }
            }
        }
        public void Clear()
        {
            Dispatcher.Invoke(() =>
            {
                contentView.Clear();
            });
        }

        private void NotifyHelper_DeleteEventTriggerModel(DeleteEventTriggerModelArgs obj)
        {
            contentView.eventSettingView.RemoveCurrentItem();

            if (File.Exists(GetSaveFilePath()))
            {
                File.Delete(GetSaveFilePath());
            }

            Dispatcher.Invoke(() =>
            {
                Save();
                Clear();
            });
        }

        private void NotifyHelper_SaveEventTriggerModel(SaveEventTriggerModelArgs obj)
        {
            var process = comboProcess.SelectedValue as Process;

            SettingProcessMonitorInfo(obj.CurrentEventTriggerModel, process);

            if (_contentController.Validate(obj.CurrentEventTriggerModel, out MessageTemplate messageTemplate))
            {
                Dispatcher.Invoke(() =>
                {
                    CacheDataManager.Instance.MakeIndexTriggerModel(obj.CurrentEventTriggerModel);

                    contentView.eventSettingView.InsertCurrentItem();
                    Save();
                    Clear();
                });
            }
            else
            {
                ApplicationManager.ShowMessageDialog("Error", messageTemplate.GetString());
            }
        }
        private void Save()
        {
            var triggers = contentView.eventSettingView.GetDataContext().TriggerSaves;
            var fileService = ServiceDispatcher.Resolve<FileService>();
            fileService.Save(GetSaveFilePath(), triggers);
        }
        private void NotifyHelper_TreeItemOrderChanged(EventTriggerOrderChangedEventArgs e)
        {
            if (File.Exists(GetSaveFilePath()))
            {
                File.Delete(GetSaveFilePath());
            }

            Save();
            Clear();
        }
        private void NotifyHelper_EventTriggerOrderChanged(EventTriggerOrderChangedEventArgs obj)
        {
            obj.SelectedTreeViewItem.IsSelected = true;

            Save();
        }

        private void ComboProcess_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            CheckFix_Checked(checkFix, null);

            if (comboProcess.SelectedItem is KeyValuePair<string, Process> item)
            {
                NotifyHelper.InvokeNotify(NotifyEventType.ComboProcessChanged, new ComboProcessChangedEventArgs()
                {
                    Process = item.Value,
                });
                var savedPosition = CacheDataManager.Instance.GetData<Rect>(item.Value);

                if (savedPosition.Equals(default) == false)
                {
                    var currentPosotion = new Rect();
                    NativeHelper.GetWindowRect(item.Value.MainWindowHandle, ref currentPosotion);

                    if (currentPosotion.Equals(savedPosition))
                    {
                        btnMoveProcessLocation.Visibility = Visibility.Visible;
                        btnRestoreMoveProcessLocation.Visibility = Visibility.Collapsed;
                    }
                    else
                    {
                        btnMoveProcessLocation.Visibility = Visibility.Collapsed;
                        btnRestoreMoveProcessLocation.Visibility = Visibility.Visible;
                    }
                }
                else
                {
                    btnMoveProcessLocation.Visibility = Visibility.Visible;
                    btnRestoreMoveProcessLocation.Visibility = Visibility.Collapsed;
                }
            }
        }
        private void CheckFix_Checked(object sender, RoutedEventArgs e)
        {
            if (sender.Equals(checkFix))
            {
                if (checkFix.IsChecked.HasValue == false)
                {
                    _contentController.SetFixProcess(null);
                    _fixProcess = null;
                    return;
                }

                if (checkFix.IsChecked.Value)
                {
                    if (comboProcess.SelectedItem is KeyValuePair<string, Process> item)
                    {
                        _fixProcess = new KeyValuePair<string, Process>(item.Key, item.Value);
                        _contentController.SetFixProcess(item.Value);

                    }
                }
                else
                {
                    _contentController.SetFixProcess(null);
                    _fixProcess = null;
                }
            }
        }
        private void NotifyHelper_SelectTreeViewChanged(SelctTreeViewItemChangedEventArgs e)
        {
            if (e.TreeViewItem == null)
            {
                return;
            }

            var model = e.TreeViewItem.DataContext<EventTriggerModel>();

            if (model == null)
            {
                Clear();
                return;
            }

            if (_fixProcess == null)
            {
                var pair = comboProcess.Items.Cast<KeyValuePair<string, Process>>().Where(r => r.Key == model.ProcessInfo.ProcessName).FirstOrDefault();
                comboProcess.SelectedValue = pair.Value;
            }
            else
            {
                comboProcess.SelectedValue = _fixProcess.Value.Value;
            }
        }

        public void LoadSaveFile(string path)
        {
            var fileManager = ServiceDispatcher.Resolve<FileService>();
            var loadDatas = fileManager.Load<EventTriggerModel>(path);
            if (loadDatas == null)
            {
                loadDatas = new List<EventTriggerModel>();
            }
            CacheDataManager.Instance.InitDatas(loadDatas);

            contentView.SaveDataBind(loadDatas);
        }

        private void NotifyHelper_ConfigChanged(ConfigEventArgs e)
        {
            ApplicationManager.ShowProgressbar();
            _config = e.Config;
            Refresh();
            LoadSaveFile(GetSaveFilePath());
            ApplicationManager.HideProgressbar();
        }
        private bool VersionCheck()
        {
            if (_config.VersionCheck == false)
            {
                return false;
            }

            var webApiManager = ServiceDispatcher.Resolve<WebApiManager>();

            var response = webApiManager.Request<GetMacroLatestVersionResponse>(new GetMacroLatestVersion());
            if (response == null)
            {
                return false;
            }
            var latestNote = response.VersionNote;

            if (latestNote.Version > VersionNote.CurrentVersion)
            {
                var newVersionTemplate = TemplateContainer<MessageTemplate>.Find(1011);

                if (ApplicationManager.ShowMessageDialog("Information", $"{newVersionTemplate.GetString()}", MessageDialogStyle.AffirmativeAndNegative) == MessageDialogResult.Affirmative)
                {
                    Process.Start(ConstHelper.VersionInfoPageUrl);
                }

                return true;
            }
            return false;
        }
        private IEnumerator ShowAd(bool isCloseButtonShow)
        {
#if DEBUG
            yield break;
#endif

            Dispatcher.Invoke(async () =>
            {
                AdOverlay.Visibility = Visibility.Visible;

                var adManager = ServiceDispatcher.Resolve<AdManager>();

                await EmbeddedWebView.LoadUrlAsync(adManager.GetRandomAdUrl());
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

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            var btn = sender as Button;
            if (btn.Equals(btnRefresh))
            {
                Refresh();
            }
            else if (btn.Equals(btnStart))
            {
                ApplicationManager.ShowProgressbar();

                var buttons = this.FindChildren<Button>();
                foreach (var button in buttons)
                {
                    if (button.Equals(btnStart) || button.Equals(btnStop))
                    {
                        continue;
                    }

                    button.IsEnabled = false;
                }
                btnStop.Visibility = Visibility.Visible;
                btnStart.Visibility = Visibility.Collapsed;
                Clear();

                _contentController.Start();

                ApplicationManager.HideProgressbar();
            }
            else if (btn.Equals(btnStop))
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
                    btnStart.Visibility = Visibility.Visible;
                    btnStop.Visibility = Visibility.Collapsed;
                });
                ApplicationManager.HideProgressbar();
            }
            else if (btn.Equals(btnSetting))
            {
                UIManager.Instance.AddPopup<SettingView>();
            }
            else if (btn.Equals(btnGithub))
            {
                Process.Start(ConstHelper.HelpUrl);
            }
            else if (btn.Equals(btnMoveProcessLocation))
            {
                if (comboProcess.SelectedValue == null)
                {
                    var template = TemplateContainer<MessageTemplate>.Find(1004);
                    ApplicationManager.ShowMessageDialog("Error", template.GetString());
                    return;
                }

                if (comboProcess.SelectedItem is KeyValuePair<string, Process> item)
                {
                    var rect = new Rect();

                    NativeHelper.GetWindowRect(item.Value.MainWindowHandle, ref rect);

                    if (CacheDataManager.Instance.GetData(item.Value) == null)
                    {
                        CacheDataManager.Instance.AddData(item.Value, rect);
                    }

                    var moveRect = new Rect
                    {
                        Left = _config.ProcessLocationX,
                        Top = _config.ProcessLocationY,
                        Bottom = _config.ProcessLocationY + rect.Height,
                        Right = _config.ProcessLocationX + rect.Width,
                    };
                    NativeHelper.SetWindowPos(item.Value.MainWindowHandle, moveRect);
                }
                else
                {
                    return;
                }

                btnMoveProcessLocation.Visibility = Visibility.Collapsed;
                btnRestoreMoveProcessLocation.Visibility = Visibility.Visible;
            }
            else if (btn.Equals(btnRestoreMoveProcessLocation))
            {
                if (comboProcess.SelectedItem is KeyValuePair<string, Process> item)
                {
                    var rect = CacheDataManager.Instance.GetData<Rect>(item.Value);
                    if (rect.Equals(default) == false)
                    {
                        NativeHelper.SetWindowPos(item.Value.MainWindowHandle, rect);
                        CacheDataManager.Instance.DeleteData(item.Value);
                    }
                }
                else
                {
                    return;
                }

                btnMoveProcessLocation.Visibility = Visibility.Visible;
                btnRestoreMoveProcessLocation.Visibility = Visibility.Collapsed;
            }
        }
    }
}

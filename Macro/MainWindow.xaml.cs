﻿using Macro.Extensions;
using Macro.Infrastructure;
using Macro.Infrastructure.Controller;
using Macro.Infrastructure.Manager;
using Macro.Models;
using Macro.View;
using MahApps.Metro.Controls;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Threading;
using Utils;
using Utils.Document;
using Utils.Extensions;
using Utils.Infrastructure;
using Label = System.Windows.Controls.Label;
using Rect = Utils.Infrastructure.Rect;
using Version = Macro.Infrastructure.Version;

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
        private string _savePath;
        private string _fullSavePath;
        private ContentView _contentView;
        private ContentController _contentController = new ContentController();
        public MainWindow()
        {
            InitializeComponent();
            Loaded += MainWindow_Loaded;
        }
        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            InitEvent();
            Init();
            VersionCheck();
        }
        private void InitEvent()
        {
            btnRefresh.Click += Button_Click;
            btnStart.Click += Button_Click;
            btnStop.Click += Button_Click;
            btnSetting.Click += Button_Click;
            btnGithub.Click += Button_Click;
            checkFix.Checked += CheckFix_Checked;
            checkFix.Unchecked += CheckFix_Checked;
            comboProcess.SelectionChanged += ComboProcess_SelectionChanged;

            NotifyHelper.ConfigChanged += NotifyHelper_ConfigChanged;
            NotifyHelper.TreeItemOrderChanged += NotifyHelper_TreeItemOrderChanged;
            NotifyHelper.SelectTreeViewChanged += NotifyHelper_SelectTreeViewChanged;
            NotifyHelper.EventTriggerOrderChanged += NotifyHelper_EventTriggerOrderChanged;
            NotifyHelper.SaveEventTriggerModel += NotifyHelper_SaveEventTriggerModel;
            NotifyHelper.DeleteEventTriggerModel += NotifyHelper_DeleteEventTriggerModel;
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
                ApplicationManager.MessageShow("Error", DocumentHelper.Get(Message.FailedOSVersion));
                Process.GetCurrentProcess().Kill();
            }
            _config = ServiceProviderManager.Instance.GetService<Config>();

            Refresh();
            //_contentView.LoadDatas(_fullSavePath);
        }
        private void RefreshPath()
        {
            _savePath = _config.SavePath;
            if (string.IsNullOrEmpty(_config.SavePath) == true)
            {
                _savePath = ConstHelper.DefaultSavePath;
            }
            else
            {
                _savePath += @"\";
            }

            _fullSavePath = $"{_savePath}\\{ConstHelper.DefaultSaveFileName}";
        }
        private void Refresh()
        {
            RefreshPath();

            if (Directory.Exists(_savePath) == false)
            {
                Directory.CreateDirectory(_savePath);
            }

            _processes = Process.GetProcesses().Where(r => r.MainWindowHandle != IntPtr.Zero)
                                                .Select(r => new KeyValuePair<string, Process>($"{r.ProcessName}", r))
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
                if (button.Equals(btnSetting) || button.Content == null || !(button.Content is string))
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
            foreach (var tab in tab_content.Items)
            {
                var tablItem = tab as TabItem;

                BindingOperations.GetBindingExpressionBase(tablItem, HeaderedContentControl.HeaderProperty).UpdateTarget();
            }
            BindingOperations.GetBindingExpressionBase(this, TitleProperty).UpdateTarget();

            Clear();

            foreach (var tab in tab_content.Items)
            {
                var tabItem = tab as TabItem;
                var tabView = tabItem.Content as ContentView;
                var key = tabView.Tag.ToString();
                if (key.Equals(_config.InitialTab.ToString()))
                {
                    tabItem.IsSelected = true;
                    _contentView = tabView;
                    break;
                }
            }

            _contentController.SetContentView(_contentView);

        }
        private void SettingProcessMonitorInfo(EventTriggerModel model, Process process)
        {
            var rect = new Rect();
            NativeHelper.GetWindowRect(process.MainWindowHandle, ref rect);
            model.ProcessInfo.Position = rect;

            if (model.EventType != EventType.Mouse)
            {
                foreach (var monitor in DisplayHelper.MonitorInfo())
                {
                    if (monitor.Rect.IsContain(rect))
                    {
                        if (model.MonitorInfo != null)
                            model.Image = model.Image.Resize((int)(model.Image.Width * (monitor.Dpi.X * 1.0F / model.MonitorInfo.Dpi.X)), (int)(model.Image.Height * (monitor.Dpi.Y * 1.0F / model.MonitorInfo.Dpi.Y)));

                        model.MonitorInfo = monitor;
                        break;
                    }
                }
            }
        }
        private void Clear()
        {
            Dispatcher.Invoke(() =>
            {
                for (int i = 0; i < tab_content.Items.Count; i++)
                {
                    if ((tab_content.Items[i] as MetroTabItem).Content is ContentView view)
                    {
                        view.Clear();
                    }
                }
            });

        }
        
        private void NotifyHelper_DeleteEventTriggerModel(DeleteEventTriggerModelArgs obj)
        {
            //if (tab_content.SelectedContent is ContentView view)
            //{
            //    var path = _viewMap[view.Tag.ToString()].SaveFilePath;
            //    _taskQueue.Enqueue(() =>
            //    {
            //        return view.Delete(path);
            //    }).ContinueWith(task =>
            //    {
            //        Clear();
            //    });
            //}
        }

        private void NotifyHelper_SaveEventTriggerModel(SaveEventTriggerModelArgs obj)
        {
            var process = comboProcess.SelectedValue as Process;

            var rect = new Rect();

            obj.CurrentEventTriggerModel.ProcessInfo = new ProcessInfo()
            {
                ProcessName = process != null ? $"{process.ProcessName}" : "",
                Position = rect
            };

            if (_contentController.Validate(obj.CurrentEventTriggerModel, out Message error))
            {
                NativeHelper.GetWindowRect(process.MainWindowHandle, ref rect);

                var path = _fullSavePath;

                Dispatcher.InvokeAsync(() =>
                {
                    var model = obj.CurrentEventTriggerModel as EventTriggerModel;
                    SettingProcessMonitorInfo(model, process);
                    _contentView.Save(_fullSavePath);
                    Clear();
                });
            }
            else
            {
                ApplicationManager.MessageShow("Error", DocumentHelper.Get(error));
            }
        }
        private void NotifyHelper_TreeItemOrderChanged(EventTriggerOrderChangedEventArgs e)
        {
            //if (tab_content.SelectedContent is ContentView view)
            //{
            //    var path = _viewMap[view.Tag.ToString()].SaveFilePath;
            //    _taskQueue.Enqueue(() =>
            //    {
            //        return view.Delete(path);
            //    }).ContinueWith(task => 
            //    {
            //        Dispatcher.Invoke(() =>
            //        {
            //            view.Clear();
            //            Clear();
            //        });
            //    }); ;
            //}
        }
        private void NotifyHelper_EventTriggerOrderChanged(EventTriggerOrderChangedEventArgs obj)
        {
            //if(tab_content.SelectedContent is ContentView view)
            //{
            //    var path = _viewMap[view.Tag.ToString()].SaveFilePath;
            //    _taskQueue.Enqueue(async () =>
            //    {
            //        await view.Save(path);
            //        obj.SelectedTreeViewItem.IsSelected = true;
            //        NotifyHelper.InvokeNotify(NotifyEventType.TreeGridViewFocus, new TreeGridViewFocusEventArgs()
            //        {
            //            Mode = (InitialTab)Enum.Parse(typeof(InitialTab), view.Tag.ToString())
            //        });
            //    });
            //}
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
            }
        }
        private void CheckFix_Checked(object sender, RoutedEventArgs e)
        {
            if (sender.Equals(checkFix))
            {
                if (checkFix.IsChecked.HasValue)
                {
                    if(checkFix.IsChecked.Value)
                    {
                        if(comboProcess.SelectedItem is KeyValuePair<string, Process> item)
                        {
                            _fixProcess = new KeyValuePair<string, Process>(item.Key, item.Value);
                        }
                    }
                    else
                    {
                        _fixProcess = null;
                    }                        
                }
                else
                {
                    _fixProcess = null;
                }
            }
        }

        private void NotifyHelper_SelectTreeViewChanged(SelctTreeViewItemChangedEventArgs e)
        {
            if(e.TreeViewItem == null)
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
                comboProcess.SelectedValue = _fixProcess.Value.Value;
        }
        private void NotifyHelper_ConfigChanged(ConfigEventArgs e)
        {
            ApplicationManager.ShowProgressbar();
            _config = e.Config;
            Refresh();
            _contentView.LoadDatas(_fullSavePath);
            settingFlyout.IsOpen = !settingFlyout.IsOpen;
            ApplicationManager.HideProgressbar();
        }

        //private async Task InvokeNextEventTriggerAsync(ContentView view, EventTriggerModel model, CancellationToken token)
        //{
        //    if (token.IsCancellationRequested)
        //    {
        //        LogHelper.Debug($"token.IsCancellationRequested!");
        //        return;
        //    }

        //    var processConfigModel = new ProcessConfigModel()
        //    {
        //        ItemDelay = config.ItemDelay,
        //        SearchImageResultDisplay = config.SearchImageResultDisplay,
        //        Processes = new List<Process>(),
        //        Token = token,
        //        Similarity = config.Similarity,
        //        DragDelay = config.DragDelay
        //    };

        //    Dispatcher.Invoke(() =>
        //    {
        //        if (fixProcess.HasValue)
        //        {
        //            processConfigModel.Processes.Add(fixProcess.Value.Value);
        //        }
        //        else
        //        {
        //            processConfigModel.Processes.AddRange(processes.Where(r => r.Key.Equals(model.ProcessInfo.ProcessName)).Select(r => r.Value));
        //        }
        //    });

        //    var nextItem = await view.InvokeNextEventTriggerAsync(model, processConfigModel);
        //    if (nextItem != null)
        //    {
        //        //await taskQueue.Enqueue(async () => await InvokeNextEventTriggerAsync(view, nextItem, token));
        //    }
        //}
        private void VersionCheck()
        {
            if (_config.VersionCheck == false)
            {
                return;
            }
            var latest = ApplicationManager.Instance.GetLatestVersion();
            if(latest != null)
            {
                if(latest.CompareTo(Version.CurrentVersion) > 0)
                {
                    if (ApplicationManager.MessageShow("Infomation", DocumentHelper.Get(Message.NewVersion), MahApps.Metro.Controls.Dialogs.MessageDialogStyle.AffirmativeAndNegative) == MahApps.Metro.Controls.Dialogs.MessageDialogResult.Affirmative)
                    {
                        if (File.Exists("Patcher.exe"))
                        {
                            var param = $"{Version.CurrentVersion.Major}.{Version.CurrentVersion.Minor}.{Version.CurrentVersion.Build} " +
                                $"{latest.Major}.{latest.Minor}.{latest.Build}";
                            Process.Start("Patcher.exe", param);
                            Application.Current.Shutdown();
                        }
                        else
                        {
                            Process.Start(ConstHelper.ReleaseUrl);
                        }
                    }
                }
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
                tab_content.IsEnabled = false;
                if(SchedulerManager.Instance.IsRunning() == false)
                {
                    SchedulerManager.Instance.Start();
                }

                ApplicationManager.HideProgressbar();
            }
            else if (btn.Equals(btnStop))
            {
                ApplicationManager.ShowProgressbar();

                SchedulerManager.Instance.Stop();

                tab_content.IsEnabled = true;
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
                settingFlyout.IsOpen = !settingFlyout.IsOpen;
            }
            else if (btn.Equals(btnGithub))
            {
                Process.Start(ConstHelper.HelpUrl);
            }
        }
    }
}

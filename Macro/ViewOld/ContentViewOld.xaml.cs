using Macro.Extensions;
using Macro.Infrastructure;
using Macro.Infrastructure.Manager;
using Macro.Models;
using System.Collections.Generic;
using System.Drawing;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Point = System.Windows.Point;

namespace Macro.View
{
    /// <summary>
    /// CommonContentView.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class ContentViewOld : UserControl
    {
        public ContentViewOld()
        {
            InitializeComponent();
            InitEvent();
        }
        public void Clear()
        {
            btnDelete.Visibility = Visibility.Collapsed;
            btnAddSameContent.Visibility = Visibility.Collapsed;
            canvasCaptureImage.Background = System.Windows.Media.Brushes.White;
            eventSettingView.Clear();
        }

        public void SaveDataBind(List<EventTriggerModel> saves)
        {
            Dispatcher.Invoke(() =>
            {
                (eventSettingView.DataContext as Models.ViewModel.EventSettingViewModel).TriggerSaves.Clear();
                foreach (var item in saves)
                {
                    (eventSettingView.DataContext as Models.ViewModel.EventSettingViewModel).TriggerSaves.Add(item);
                }
            });
        }
        public void DrawCaptureImage(Bitmap bmp)
        {
            Dispatcher.Invoke(() =>
            {
                canvasCaptureImage.Background = new ImageBrush(bmp.ToBitmapSource());
            });
        }

        private void InitEvent()
        {
            btnSave.Click += Button_Click;
            btnDelete.Click += Button_Click;
            btnAddSameContent.Click += Button_Click;
            btnCapture.Click += Button_Click;

            Loaded += ContentView_Loaded;

            NotifyHelperOld.ScreenCaptureCompleted += ScreenCaptureCompleted;
            NotifyHelperOld.ROICaptureCompleted += ROICaptureCompleted;
            NotifyHelperOld.SelectTreeViewChanged += NotifyHelper_SelectTreeViewChanged;
        }
        private void ContentView_Loaded(object sender, RoutedEventArgs e)
        {
            Init();
        }

        private void Init()
        {
        }
        private void ROICaptureCompleted(ROICaptureCompletedEventArgs e)
        {
            ApplicationManager.Instance.CloseCaptureView();
            var dataContext = eventSettingView.GetDataContext();
            var model = dataContext.CurrentTreeViewItem.DataContext<EventTriggerModel>();

            model.RoiDataInfo = new RoiModel()
            {
                RoiRect = e.RoiRect,
                MonitorInfo = e.MonitorInfo
            };
        }

        private void ScreenCaptureCompleted(CaptureCompletedEventArgs e)
        {
            ApplicationManager.Instance.CloseCaptureView();

            if (e.CaptureImage != null)
            {
                var capture = e.CaptureImage;
                canvasCaptureImage.Background = new ImageBrush(capture.ToBitmapSource());

                var dataContext = eventSettingView.GetDataContext();
                var model = dataContext.CurrentTreeViewItem.DataContext<EventTriggerModel>();

                model.Image = new Bitmap(capture, capture.Width, capture.Height);
                model.MonitorInfo = e.MonitorInfo;
            }
        }
        private void NotifyHelper_SelectTreeViewChanged(SelctTreeViewItemChangedEventArgs e)
        {
            if (e.TreeViewItem == null)
            {
                Clear();
                return;
            }
            else if (e.TreeViewItem.DataContext<EventTriggerModel>() == null)
            {
                return;
            }
            else
            {
                var model = e.TreeViewItem.DataContext<EventTriggerModel>();
                btnDelete.Visibility = Visibility.Visible;
                btnAddSameContent.Visibility = Visibility.Visible;
                canvasCaptureImage.Background = new ImageBrush(model.Image.ToBitmapSource());
            }
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            var btn = sender as Button;
            if (btn.Equals(btnCapture))
            {
                var dataContext = eventSettingView.GetDataContext();
                if (dataContext.IsExistence() == true)
                {
                    dataContext.Clear();
                }
                ApplicationManager.Instance.ShowCaptureImageView();
            }
            else if (btn.Equals(btnSave))
            {
                var dataContext = eventSettingView.GetDataContext();
                var model = dataContext.CurrentTreeViewItem.DataContext<EventTriggerModel>();
                if (model.EventType == EventType.RelativeToImage)
                {
                    model.MouseEventInfo.StartPoint = new Point(dataContext.RelativePosition.X, dataContext.RelativePosition.Y);
                }

                NotifyHelperOld.InvokeNotify(NotifyEventOldType.Save, new SaveEventTriggerModelArgs()
                {
                    CurrentEventTriggerModel = model,
                });
            }
            else if (btn.Equals(btnAddSameContent))
            {
                var item = eventSettingView.CopyCurrentItem();
                if (item == null)
                {
                    return;
                }

                NotifyHelperOld.InvokeNotify(NotifyEventOldType.Save, new SaveEventTriggerModelArgs()
                {
                    CurrentEventTriggerModel = item,
                });
            }
            else if (btn.Equals(btnDelete))
            {
                var model = eventSettingView.GetDataContext().CurrentTreeViewItem.DataContext<EventTriggerModel>();
                NotifyHelperOld.InvokeNotify(NotifyEventOldType.Delete, new DeleteEventTriggerModelArgs()
                {
                    CurrentEventTriggerModel = model,
                });
            }
        }
    }
}

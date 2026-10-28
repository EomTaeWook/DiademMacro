using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Macro.Models.ViewModel;
using Macro.Infrastructure.Manager;

namespace Macro.View
{
    public class AppUpdateView : UserControl
    {
        public AppUpdateView()
        {
            AvaloniaXamlLoader.Load(this);
            this.FindControl<Button>("checkButton").Click += (sender, eventArgs) => UIManager.Instance.EnqueueButtonAction(((AppUpdateViewModel)DataContext).CheckAsync);
            this.FindControl<Button>("installButton").Click += (sender, eventArgs) => UIManager.Instance.EnqueueButtonAction(((AppUpdateViewModel)DataContext).InstallAsync);
            this.FindControl<Button>("releaseButton").Click += (sender, eventArgs) => UIManager.Instance.EnqueueButtonAction(((AppUpdateViewModel)DataContext).OpenReleasePage);
            this.FindControl<Button>("cancelButton").Click += (sender, eventArgs) => ((AppUpdateViewModel)DataContext).Cancel();
        }
    }
}

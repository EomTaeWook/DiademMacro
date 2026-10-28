using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Macro.Models.ViewModel;

namespace Macro.View
{
    public class AppUpdateView : UserControl
    {
        public AppUpdateView()
        {
            AvaloniaXamlLoader.Load(this);
            this.FindControl<Button>("checkButton").Click += async (sender, eventArgs) => await ((AppUpdateViewModel)DataContext).CheckAsync();
            this.FindControl<Button>("installButton").Click += async (sender, eventArgs) => await ((AppUpdateViewModel)DataContext).InstallAsync();
            this.FindControl<Button>("releaseButton").Click += (sender, eventArgs) => ((AppUpdateViewModel)DataContext).OpenReleasePage();
            this.FindControl<Button>("cancelButton").Click += (sender, eventArgs) => ((AppUpdateViewModel)DataContext).Cancel();
        }
    }
}

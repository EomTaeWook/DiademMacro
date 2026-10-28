using Avalonia.Controls;
using Avalonia.Platform.Storage;
using Dignus.Log;
using Macro.Infrastructure.Manager;
using System;
using System.Threading.Tasks;
using System.Windows.Input;

namespace Macro.Command
{
    public class FolderBrowserDialogCommand : ICommand
    {
        public event EventHandler CanExecuteChanged
        {
            add { }
            remove { }
        }

        public bool CanExecute(object parameter)
        {
            return true;
        }

        public void Execute(object parameter)
        {
            if (parameter is TextBox textBox == false)
            {
                return;
            }
            if (textBox.IsEffectivelyEnabled == false)
            {
                return;
            }
            UIManager.Instance.EnqueueButtonAction(() => SelectFolderAsync(textBox));
        }

        private async Task SelectFolderAsync(TextBox textBox)
        {
            try
            {
                TopLevel topLevel = TopLevel.GetTopLevel(textBox);
                if (topLevel == null)
                {
                    return;
                }
                if (topLevel.IsEnabled == false)
                {
                    return;
                }
                var folders = await topLevel.StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions { AllowMultiple = false });
                if (folders.Count == 0)
                {
                    return;
                }
                string selectedPath = folders[0].TryGetLocalPath();
                if (TopLevel.GetTopLevel(textBox) != topLevel)
                {
                    return;
                }
                if (selectedPath != null)
                {
                    textBox.SetCurrentValue(TextBox.TextProperty, selectedPath);
                }
            }
            catch (Exception exception)
            {
                LogHelper.Error(exception);
            }
        }
    }
}

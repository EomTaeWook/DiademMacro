using Avalonia.Controls;
using Avalonia.Platform.Storage;
using Dignus.Log;
using System;
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

        public async void Execute(object parameter)
        {
            if (parameter is TextBox textBox == false)
            {
                return;
            }
            try
            {
                TopLevel topLevel = TopLevel.GetTopLevel(textBox);
                var folders = await topLevel.StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions { AllowMultiple = false });
                if (folders.Count == 0)
                {
                    return;
                }
                string selectedPath = folders[0].TryGetLocalPath();
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

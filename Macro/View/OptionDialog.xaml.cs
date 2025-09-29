using Macro.Models.ViewModel;
using System.Windows;

namespace Macro.View
{
    /// <summary>
    /// OptionDialog.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class OptionDialog : Window
    {
        public OptionDialog()
        {
            InitializeComponent();
            DataContext = new OptionDialogViewModel();
        }
    }
}

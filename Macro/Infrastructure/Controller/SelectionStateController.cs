using Dignus.DependencyInjection.Attributes;
using Macro.Models;
using Macro.UI;

namespace Macro.Infrastructure.Controller
{
    [Injectable(Dignus.DependencyInjection.LifeScope.Singleton)]
    internal class SelectionStateController
    {
        public ProcessItem SelectProcessItem { get; set; }
        public TreeGridViewItem SelectTreeGridViewItem { get; set; }

        public void UnselectTreeGridViewItem()
        {
            if (SelectTreeGridViewItem != null)
            {
                SelectTreeGridViewItem.IsSelected = false;
                SelectTreeGridViewItem = null;
            }
        }
    }
}

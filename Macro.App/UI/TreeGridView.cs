using Avalonia.Controls;
using System;

namespace Macro.UI
{
    public class TreeGridView : TreeView
    {
        protected override Type StyleKeyOverride
        {
            get
            {
                return typeof(TreeView);
            }
        }

        protected override Control CreateContainerForItemOverride(object item, int index, object recycleKey)
        {
            return new TreeGridViewItem();
        }

        protected override bool NeedsContainerOverride(object item, int index, out object recycleKey)
        {
            return NeedsContainer<TreeGridViewItem>(item, out recycleKey);
        }
    }
}

using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.VisualTree;
using System;
using System.Linq;

namespace Macro.UI
{
    public class TreeGridViewItem : TreeViewItem
    {
        public TreeGridViewItem ParentItem
        {
            get
            {
                return this.GetVisualAncestors().OfType<TreeGridViewItem>().FirstOrDefault();
            }
        }

        protected override Type StyleKeyOverride
        {
            get
            {
                return typeof(TreeViewItem);
            }
        }

        public TreeGridViewItem()
        {
            AddHandler(DragDrop.DragEnterEvent, DragEntered);
            AddHandler(DragDrop.DragLeaveEvent, DragLeft);
            AddHandler(DragDrop.DropEvent, DragLeft);
            PointerExited += PointerLeft;
        }

        private void DragEntered(object sender, DragEventArgs eventArgs)
        {
            Classes.Add("dragTarget");
        }

        private void DragLeft(object sender, DragEventArgs eventArgs)
        {
            Classes.Remove("dragTarget");
        }

        private void PointerLeft(object sender, PointerEventArgs eventArgs)
        {
            Classes.Remove("dragTarget");
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

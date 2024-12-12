using System;
using System.Collections;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Ssz.Operator.Core.ControlsCommon;
using Ssz.Utils.MonitoredUndo;
using Ssz.Xceed.Wpf.Toolkit;
using Ssz.Xceed.Wpf.Toolkit.PropertyGrid;
using Ssz.Xceed.Wpf.Toolkit.PropertyGrid.Editors;

namespace Ssz.Operator.Core.VisualEditors.PropertyGridTypeEditors
{
    public partial class MiscTypeCloneableObjectsCollectionTypeEditor : UserControl, ITypeEditor
    {
        #region private fields

        private PropertyItem? _item;

        #endregion

        #region construction and destruction

        public MiscTypeCloneableObjectsCollectionTypeEditor()
        {
            InitializeComponent();
        }

        #endregion

        #region public functions

        public FrameworkElement ResolveEditor(PropertyItem propertyItem)
        {
            _item = propertyItem;
            return this;
        }

        #endregion

        #region private functions

        private void ButtonClick(object? sender, RoutedEventArgs e)
        {
            if (_item is null) throw new InvalidOperationException();
            var editor = new CollectionControlDialog(_item.PropertyType, _item.DescriptorDefinition.NewItemTypes);
            WindowBehavior.SetHideCloseButton(editor, true);
            var originalCollection = (IList) _item.Value;
            editor.ItemsSource = originalCollection.OfType<ICloneable>().ToList();
            editor.ShowDialog();
            if (editor.DialogResult == true)
            {
                UndoService.Current.BeginChangeSetBatch("Cloneable Objects", true);

                for (var i = originalCollection.Count - 1; i >= 0; i--) originalCollection.RemoveAt(i);
                foreach (object item in editor.ItemsSource) originalCollection.Add(item);

                UndoService.Current.EndChangeSetBatch();
            }
        }

        #endregion
    }
}
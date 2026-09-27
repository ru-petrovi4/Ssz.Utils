using System;
using Avalonia.Controls;
using Avalonia.Threading;
using Avalonia.VisualTree;
using System.Linq;

namespace Ssz.Operator.Core.VisualEditors
{
    /// <summary>
    ///     The properties of one object, for a dialog to show. Ported from the WPF editor, including
    ///     the two second refresh that keeps what is shown in step with what the object holds.
    /// </summary>
    public partial class ObjectPropertiesControl : UserControl
    {
        #region construction and destruction

        public ObjectPropertiesControl()
        {
            InitializeComponent();

            Dispatcher.UIThread.Post(BeginEditing, DispatcherPriority.Background);

            _dispatcherTimer = new DispatcherTimer(TimeSpan.FromSeconds(2), DispatcherPriority.Background,
                (sender, e) => Refresh());
            _dispatcherTimer.Start();

            Unloaded += (sender, e) =>
            {
                _dispatcherTimer.Stop();
                ObjectPropertyGrid.SelectedObject = null;
            };
        }

        #endregion

        #region public functions

        public object? SelectedObject
        {
            get
            {
                EndEditing();
                return ObjectPropertyGrid.SelectedObject;
            }
            set
            {
                ObjectPropertyGrid.SelectedObject = value;
                ObjectPropertyGrid.SelectedObjectTypeName = value?.ToString() ?? @"";
                ObjectPropertyGrid.SelectedObjectName = @"";
            }
        }

        #endregion

        #region private functions

        private void BeginEditing()
        {
            var item = ObjectPropertyGrid.SelectedObject as IPropertyGridItem;
            if (item is null || item.RefreshForPropertyGridIsDisabled) return;

            item.RefreshForPropertyGrid();
        }

        private void Refresh()
        {
            var item = ObjectPropertyGrid.SelectedObject as IPropertyGridItem;
            if (item is null || item.RefreshForPropertyGridIsDisabled) return;

            foreach (IPropertyGridItem child in this.GetVisualDescendants().OfType<IPropertyGridItem>())
                child.RefreshForPropertyGrid();

            item.RefreshForPropertyGrid();

            ObjectPropertyGrid.RefreshValues();
        }

        private void EndEditing()
        {
            ObjectPropertyGrid.EndEditInPropertyGrid();

            var item = ObjectPropertyGrid.SelectedObject as IPropertyGridItem;
            if (item is null || item.RefreshForPropertyGridIsDisabled) return;

            item.RefreshForPropertyGrid();
        }

        #endregion

        #region private fields

        private readonly DispatcherTimer _dispatcherTimer;

        #endregion
    }
}

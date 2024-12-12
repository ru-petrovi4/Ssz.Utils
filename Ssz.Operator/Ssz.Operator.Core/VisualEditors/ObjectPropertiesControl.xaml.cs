using System;
using System.Windows.Controls;
using System.Windows.Threading;
using Ssz.Utils.Wpf;
using Ssz.Xceed.Wpf.Toolkit.PropertyGrid.Implementation.Editors;

namespace Ssz.Operator.Core.VisualEditors
{
    public partial class ObjectPropertiesControl : UserControl
    {
        #region private fields

        private readonly DispatcherTimer _dispatcherTimer;

        #endregion

        #region construction and destruction

        public ObjectPropertiesControl()
        {
            InitializeComponent();

            Dispatcher.BeginInvoke(DispatcherPriority.Background, new Action(BeginEditing));

            _dispatcherTimer = new DispatcherTimer(new TimeSpan(0, 0, 2), DispatcherPriority.Background,
                (sender, e) => Refresh(),
                Dispatcher);

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
                ObjectPropertyGrid.SelectedObjectTypeName = value?.ToString() ?? "";
                ObjectPropertyGrid.SelectedObjectName = "";
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

            foreach (IPropertyGridItem child in TreeHelper.FindChilds<IPropertyGridItem>(this))
                child.RefreshForPropertyGrid();

            item.RefreshForPropertyGrid();
        }

        private void EndEditing()
        {
            ObjectPropertyGrid.EndEditInPropertyGrid();

            var item = ObjectPropertyGrid.SelectedObject as IPropertyGridItem;
            if (item is null || item.RefreshForPropertyGridIsDisabled) return;

            item.RefreshForPropertyGrid();
        }

        #endregion
    }
}
using Ssz.Operator.Core.Commands;
using Ssz.Operator.Core.ControlsPlay;
using Ssz.Operator.Core.Drawings;
using Ssz.Operator.Core.DsShapes;
using System;
using System.Collections;
using System.Collections.Generic;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Data.Converters;
using Avalonia.Data;
using Avalonia.Threading;

namespace Ssz.Operator.Core.DsShapeViews
{
    public class TabDsShapeView : ControlDsShapeView<TabControl>
    {
        #region construction and destruction

        public TabDsShapeView(TabDsShape DsShape, ControlsPlay.Frame? frame)
            : base(new TabControl(), DsShape, frame)
        {            
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                DisposeItemsSource(Control.ItemsSource);                
            }

            base.Dispose(disposing);
        }

        #endregion

        #region protected functions       

        protected override void OnDsShapeChanged(string? propertyName)
        {
            base.OnDsShapeChanged(propertyName);

            var dsShape = (TabDsShape)DsShapeViewModel.DsShape;
            if (propertyName is null || propertyName == nameof(dsShape.HorizontalContentAlignment))
                Control.SetConst(dsShape.Container,
                    TabControl.HorizontalContentAlignmentProperty,
                    dsShape.HorizontalContentAlignment);
            if (propertyName is null || propertyName == nameof(dsShape.VerticalContentAlignment))
                Control.SetConst(dsShape.Container,
                    TabControl.VerticalContentAlignmentProperty,
                    dsShape.VerticalContentAlignment);
            if (propertyName == null || propertyName == nameof(dsShape.TabItemInfosCollection))
            {
                DisposeItemsSource(Control.ItemsSource);
                Control.ItemsSource = GetTabItems();
            }            
        }

        private void DisposeItemsSource(IEnumerable? itemsSource)
        {
            if (itemsSource == null)
                return;
            foreach (TabItem tabItem in itemsSource)
            {
                var disposable = tabItem.Content as IDisposable;
                if (disposable != null) disposable.Dispose();
            }            
        }

        #endregion

        #region private functions

        private IEnumerable<object> GetTabItems()
        {
            var dsShape = (TabDsShape)DsShapeViewModel.DsShape;
            var container = dsShape.Container;
            var result = new List<object>();
            foreach (ICloneable item in dsShape.TabItemInfosCollection)
            {                
                var tabItemInfo = item as TabItemInfo;
                if (tabItemInfo != null)
                {
                    tabItemInfo.ParentItem = dsShape;
                    tabItemInfo.DsCommand.ParentItem = dsShape;
                    var newTabItem = new TabItem();
                    newTabItem.DataContext = DataContext;
                    newTabItem.SetBindingOrConst(container, TabItem.HeaderProperty, tabItemInfo.HeaderInfo,
                        BindingMode.OneWay,
                        UpdateSourceTrigger.Default);

                    newTabItem.SetVisibilityBindingOrConst(container, TabItem.IsVisibleProperty, tabItemInfo.IsVisibleInfo,
                        false,
                        VisualDesignMode);

                    if (!tabItemInfo.DsCommand.IsEmpty)
                    {
                        newTabItem.Tag = new DsCommandView(Frame, tabItemInfo.DsCommand, DsShapeViewModel);
                        newTabItem.PointerReleased +=
                            (sender, args) =>
                                Dispatcher.UIThread.InvokeAsync(new Action(((DsCommandView)((Control)sender!).Tag!).DoCommand));
                    }                    

                    SetContent(newTabItem, tabItemInfo.PageFileRelativePath);

                    result.Add(newTabItem);
                }
            }
            return result.ToArray();
        }

        #endregion

        #region private functions

        private async void SetContent(TabItem newTabItem, string pageFileRelativePath)
        {
            var playWindow = Frame != null ? Frame.PlayWindow : null;
            DsPageDrawing? pageDrawing = await DsProject.Instance.ReadDsPageInPlayAsync(
                pageFileRelativePath,
                DsShapeViewModel.DsShape.ParentItem.Find<IDsContainer>(),
                playWindow);
            if (pageDrawing != null)
                newTabItem.Content = new PlayDsPageDrawingViewbox(pageDrawing, Frame);
        }

        #endregion
    }
}
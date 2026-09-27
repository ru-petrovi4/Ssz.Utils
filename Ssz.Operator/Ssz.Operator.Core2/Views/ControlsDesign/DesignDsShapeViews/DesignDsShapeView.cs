using System;
using System.Globalization;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Templates;
using Avalonia.Data;
using Avalonia.Data.Converters;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using Ssz.Operator.Core.DsShapeViews;

namespace Ssz.Operator.Core.ControlsDesign
{
    /// <summary>
    ///     What the editor wraps a shape view in: the drag handle over it and the resize handles around
    ///     it. The shape view itself is the content.
    /// </summary>
    public class DesignDsShapeView : ContentControl, IDisposable
    {
        #region construction and destruction

        public DesignDsShapeView(DsShapeViewBase dsShapeView, DesignDrawingCanvas designerDrawingCanvas)
        {
            DsShapeView = dsShapeView;
            DesignDrawingCanvas = designerDrawingCanvas;

            TransformGroup.Children.Add(ScaleTranform);
            TransformGroup.Children.Add(RotateTransform);
            RenderTransform = TransformGroup;

            DataContext = dsShapeView.DsShapeViewModel;

            // The handles sit just outside the shape, so nothing along the way may cut them off.
            ClipToBounds = false;

            MinWidth = 1;
            MinHeight = 1;
            Template = new FuncControlTemplate<DesignDsShapeView>(BuildVisual);

            ContextMenuHelper.Attach(this, ContextMenuHelper.DesignDsShapeContextMenuKey);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (Disposed) return;

            Disposed = true;
        }

        ~DesignDsShapeView()
        {
            Dispose(false);
        }

        #endregion

        #region public functions

        public DesignDrawingCanvas DesignDrawingCanvas { get; }

        public DsShapeViewBase DsShapeView { get; }

        public DsShapeViewModel DsShapeViewModel => (DsShapeViewModel) DataContext!;

        public readonly RotateTransform RotateTransform = new();
        public readonly ScaleTransform ScaleTranform = new();
        public readonly TransformGroup TransformGroup = new();

        public const double ResizeThumbThikness = 6.0;

        #endregion

        #region private functions

        /// <summary>
        ///     What the editor puts over a selected shape: the handle that moves it, the content of the
        ///     view itself, and the eight handles that resize it.
        ///     <para>
        ///         Ported from the WPF editor's DesignDsShapeViewStyle, which said the same thing as a
        ///         ControlTemplate. Its grid took the shape view as its data context, which is where
        ///         both kinds of thumb read the shape they act on, so the grid does that here too.
        ///     </para>
        /// </summary>
        private static Control BuildVisual(DesignDsShapeView designDsShapeView, INameScope nameScope)
        {
            var grid = new Grid { DataContext = designDsShapeView, ClipToBounds = false };

            var dragThumb = new DesignDsShapeViewDragThumb
            {
                Name = @"PART_DragThumb",
                Cursor = new Cursor(StandardCursorType.Hand)
            };
            dragThumb.RegisterInNameScope(nameScope);
            grid.Children.Add(dragThumb);

            var contentPresenter = new ContentPresenter
            {
                Name = @"PART_ContentPresenter",
                HorizontalAlignment = HorizontalAlignment.Stretch,
                VerticalAlignment = VerticalAlignment.Stretch
            };
            contentPresenter.Bind(ContentPresenter.ContentProperty, new Binding
            {
                Source = designDsShapeView,
                Path = nameof(Content),
                Mode = BindingMode.OneWay
            });
            contentPresenter.Bind(ContentPresenter.MarginProperty, new Binding
            {
                Source = designDsShapeView,
                Path = nameof(Padding),
                Mode = BindingMode.OneWay
            });
            contentPresenter.RegisterInNameScope(nameScope);
            grid.Children.Add(contentPresenter);

            Control resizeDecorator = BuildResizeDecorator(designDsShapeView);
            resizeDecorator.Name = @"PART_ResizeDecorator";
            resizeDecorator.RegisterInNameScope(nameScope);
            grid.Children.Add(resizeDecorator);

            // A connector has no size of its own to drag or to resize - it follows what it joins.
            if (!designDsShapeView.DsShapeViewModel.ResizeDecoratorIsVisible)
            {
                dragThumb.IsVisible = false;
                resizeDecorator.IsVisible = false;
            }

            return grid;
        }

        /// <summary>
        ///     The eight handles around the shape. Which edge or corner one sits on is its alignment,
        ///     and that is what it reads to know which way the shape grows.
        /// </summary>
        private static Control BuildResizeDecorator(DesignDsShapeView designDsShapeView)
        {
            var grid = new Grid { ClipToBounds = false };

            void Add(double width, double height, HorizontalAlignment h, VerticalAlignment v,
                Thickness margin, StandardCursorType cursor)
            {
                var thumb = new DesignDsShapeViewResizeThumb
                {
                    DataContext = designDsShapeView,
                    HorizontalAlignment = h,
                    VerticalAlignment = v,
                    Margin = margin,
                    Cursor = new Cursor(cursor)
                };
                if (!Double.IsNaN(width)) thumb.Width = width;
                if (!Double.IsNaN(height)) thumb.Height = height;
                grid.Children.Add(thumb);
            }

            Add(Double.NaN, 4, HorizontalAlignment.Stretch, VerticalAlignment.Top,
                new Thickness(0, -4, 0, 0), StandardCursorType.SizeNorthSouth);
            Add(4, Double.NaN, HorizontalAlignment.Left, VerticalAlignment.Stretch,
                new Thickness(-4, 0, 0, 0), StandardCursorType.SizeWestEast);
            Add(4, Double.NaN, HorizontalAlignment.Right, VerticalAlignment.Stretch,
                new Thickness(0, 0, -4, 0), StandardCursorType.SizeWestEast);
            Add(Double.NaN, 4, HorizontalAlignment.Stretch, VerticalAlignment.Bottom,
                new Thickness(0, 0, 0, -4), StandardCursorType.SizeNorthSouth);
            Add(ResizeThumbThikness, ResizeThumbThikness, HorizontalAlignment.Left, VerticalAlignment.Top,
                new Thickness(-ResizeThumbThikness, -ResizeThumbThikness, 0, 0),
                StandardCursorType.TopLeftCorner);
            Add(ResizeThumbThikness, ResizeThumbThikness, HorizontalAlignment.Right, VerticalAlignment.Top,
                new Thickness(0, -ResizeThumbThikness, -ResizeThumbThikness, 0),
                StandardCursorType.TopRightCorner);
            Add(ResizeThumbThikness, ResizeThumbThikness, HorizontalAlignment.Left, VerticalAlignment.Bottom,
                new Thickness(-ResizeThumbThikness, 0, 0, -ResizeThumbThikness),
                StandardCursorType.BottomLeftCorner);
            Add(ResizeThumbThikness, ResizeThumbThikness, HorizontalAlignment.Right, VerticalAlignment.Bottom,
                new Thickness(0, 0, -ResizeThumbThikness, -ResizeThumbThikness),
                StandardCursorType.BottomRightCorner);

            grid.Bind(OpacityProperty, new Binding
            {
                Source = designDsShapeView.DsShapeViewModel,
                Path = nameof(DsShapeViewModel.IsFirstSelected),
                Mode = BindingMode.OneWay,
                Converter = FirstSelectedToOpacityConverter
            });

            return grid;
        }

        /// <summary>
        ///     Anything but a first selected shape gets fainter handles. A value that is not yet there
        ///     is treated as not first selected rather than as nothing, so the handles never vanish.
        /// </summary>
        private class FirstSelectedToOpacity : IValueConverter
        {
            public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
            {
                return value is true ? 0.7 : 0.35;
            }

            public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
            {
                throw new NotSupportedException();
            }
        }

        private static readonly IValueConverter FirstSelectedToOpacityConverter = new FirstSelectedToOpacity();

        #endregion

        #region protected functions

        protected bool Disposed { get; private set; }

        /// <summary>
        ///     The three derived views share one control theme, so they are all styled as this type.
        /// </summary>
        protected override Type StyleKeyOverride => typeof(DesignDsShapeView);

        #endregion
    }
}

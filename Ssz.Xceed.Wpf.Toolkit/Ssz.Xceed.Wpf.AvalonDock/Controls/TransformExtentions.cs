/*************************************************************************************
   
   Toolkit for WPF

   Copyright (C) 2007-2018 Ssz.Xceed Software Inc.

   This program is provided to you under the terms of the Microsoft Public
   License (Ms-PL) as published at http://wpftoolkit.codeplex.com/license 

   For more features, controls, and fast professional support,
   pick up the Plus Edition at https://xceed.com/xceed-toolkit-plus-for-wpf/

   Stay informed: follow @datagrid on Twitter or Like http://facebook.com/datagrids

  ***********************************************************************************/

using System.Windows;
using System.Windows.Media;

namespace Ssz.Xceed.Wpf.AvalonDock.Controls
{
    internal static class TransformExtensions
    {
        public static Point PointToScreenDPI(this Visual visual, Point pt)
        {
            var resultPt = visual.PointToScreen(pt);
            return TransformToDeviceDPI(visual, resultPt);
        }

        public static Point PointToScreenDPIWithoutFlowDirection(this FrameworkElement element, Point point)
        {
            if (FrameworkElement.GetFlowDirection(element) == FlowDirection.RightToLeft)
            {
                var actualSize = element.TransformActualSizeToAncestor();
                var leftToRightPoint = new Point(
                    actualSize.Width - point.X,
                    point.Y);
                return element.PointToScreenDPI(leftToRightPoint);
            }

            return element.PointToScreenDPI(point);
        }

        public static Rect GetScreenArea(this FrameworkElement element)
        {
            //    return new Rect(element.PointToScreenDPI(new Point()),
            //        element.TransformActualSizeToAncestor());
            //}

            //public static Rect GetScreenAreaWithoutFlowDirection(this FrameworkElement element)
            //{
            var point = element.PointToScreenDPI(new Point());
            if (FrameworkElement.GetFlowDirection(element) == FlowDirection.RightToLeft)
            {
                var actualSize = element.TransformActualSizeToAncestor();
                var leftToRightPoint = new Point(
                    actualSize.Width - point.X,
                    point.Y);
                return new Rect(leftToRightPoint,
                    actualSize);
            }

            return new Rect(point,
                element.TransformActualSizeToAncestor());
        }

        public static Point TransformToDeviceDPI(this Visual visual, Point pt)
        {
            var m = PresentationSource.FromVisual(visual).CompositionTarget.TransformToDevice;
            return new Point(pt.X / m.M11, pt.Y / m.M22);
        }

        public static Size TransformFromDeviceDPI(this Visual visual, Size size)
        {
            var m = PresentationSource.FromVisual(visual).CompositionTarget.TransformToDevice;
            return new Size(size.Width * m.M11, size.Height * m.M22);
        }

        public static Point TransformFromDeviceDPI(this Visual visual, Point pt)
        {
            var m = PresentationSource.FromVisual(visual).CompositionTarget.TransformToDevice;
            return new Point(pt.X * m.M11, pt.Y * m.M22);
        }

        public static bool CanTransform(this Visual visual)
        {
            return PresentationSource.FromVisual(visual) is not null;
        }

        public static Size TransformActualSizeToAncestor(this FrameworkElement element)
        {
            if (PresentationSource.FromVisual(element) is null)
                return new Size(element.ActualWidth, element.ActualHeight);

            var parentWindow = PresentationSource.FromVisual(element).RootVisual;
            var transformToWindow = element.TransformToAncestor(parentWindow);
            return transformToWindow.TransformBounds(new Rect(0, 0, element.ActualWidth, element.ActualHeight)).Size;
        }

        public static Size TransformSizeToAncestor(this FrameworkElement element, Size sizeToTransform)
        {
            if (PresentationSource.FromVisual(element) is null)
                return sizeToTransform;

            var parentWindow = PresentationSource.FromVisual(element).RootVisual;
            var transformToWindow = element.TransformToAncestor(parentWindow);
            return transformToWindow.TransformBounds(new Rect(0, 0, sizeToTransform.Width, sizeToTransform.Height))
                .Size;
        }

        public static GeneralTransform TansformToAncestor(this FrameworkElement element)
        {
            if (PresentationSource.FromVisual(element) is null)
                return new MatrixTransform(Matrix.Identity);

            var parentWindow = PresentationSource.FromVisual(element).RootVisual;
            return element.TransformToAncestor(parentWindow);
        }
    }
}
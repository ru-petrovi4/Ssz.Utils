﻿// --------------------------------------------------------------------------------------------------------------------
// <copyright file="ConverterExtensions.cs" company="OxyPlot">
//   Copyright (c) 2014 OxyPlot contributors
// </copyright>
// <summary>
//   Extension method used to convert to/from Windows/Windows.Media classes.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace OxyPlot.Avalonia
{
    using global::Avalonia;
    using global::Avalonia.Input;
    using global::Avalonia.Media;
    using global::Avalonia.Media.Immutable;
    using System;

    /// <summary>
    /// Extension method used to convert to/from Windows/Windows.Media classes.
    /// </summary>
    public static class ConverterExtensions
    {
        /// <summary>
        /// Calculate the distance between two points.
        /// </summary>
        /// <param name="p1">The first point.</param>
        /// <param name="p2">The second point.</param>
        /// <returns>The distance.</returns>
        public static double DistanceTo(this Point p1, Point p2)
        {
            var dx = p1.X - p2.X;
            var dy = p1.Y - p2.Y;
            return Math.Sqrt((dx * dx) + (dy * dy));
        }

        /// <summary>
        /// Converts an <see cref="OxyColor" /> to a <see cref="Brush" />.
        /// </summary>
        /// <param name="c">The color.</param>
        /// <returns>A <see cref="SolidColorBrush" />.</returns>
        public static IBrush ToBrush(this OxyColor c)
        {
            return !c.IsUndefined() ? new SolidColorBrush(c.ToColor()) : null;
        }

        /// <summary>
        /// Converts an <see cref="OxyColor" /> to a <see cref="Color" />.
        /// </summary>
        /// <param name="c">The color.</param>
        /// <returns>A Color.</returns>
        public static Color ToColor(this OxyColor c)
        {
            return Color.FromArgb(c.A, c.R, c.G, c.B);
        }

        /// <summary>
        /// Converts an OxyThickness to a Thickness.
        /// </summary>
        /// <param name="c">The thickness.</param>
        /// <returns>A <see cref="Thickness" /> instance.</returns>
        public static Thickness ToThickness(this OxyThickness c)
        {
            return new Thickness(c.Left, c.Top, c.Right, c.Bottom);
        }

        /// <summary>
        /// Converts a ScreenVector to a Vector.
        /// </summary>
        /// <param name="c">The c.</param>
        /// <returns>A <see cref="Vector" /> instance.</returns>
        public static Vector ToVector(this ScreenVector c)
        {
            return new Vector(c.X, c.Y);
        }

        /// <summary>
        /// Converts a HorizontalAlignment to a HorizontalAlignment.
        /// </summary>
        /// <param name="alignment">The alignment.</param>
        /// <returns>A HorizontalAlignment.</returns>
        public static HorizontalAlignment ToHorizontalAlignment(this global::Avalonia.Layout.HorizontalAlignment alignment)
        {
            switch (alignment)
            {
                case global::Avalonia.Layout.HorizontalAlignment.Center:
                    return HorizontalAlignment.Center;
                case global::Avalonia.Layout.HorizontalAlignment.Right:
                    return HorizontalAlignment.Right;
                default:
                    return HorizontalAlignment.Left;
            }
        }

        /// <summary>
        /// Converts a HorizontalAlignment to a VerticalAlignment.
        /// </summary>
        /// <param name="alignment">The alignment.</param>
        /// <returns>A VerticalAlignment.</returns>
        public static VerticalAlignment ToVerticalAlignment(this global::Avalonia.Layout.VerticalAlignment alignment)
        {
            switch (alignment)
            {
                case global::Avalonia.Layout.VerticalAlignment.Center:
                    return VerticalAlignment.Middle;
                case global::Avalonia.Layout.VerticalAlignment.Top:
                    return VerticalAlignment.Top;
                default:
                    return VerticalAlignment.Bottom;
            }
        }

        /// <summary>
        /// Converts a Color to an OxyColor.
        /// </summary>
        /// <param name="color">The color.</param>
        /// <returns>An OxyColor.</returns>
        public static OxyColor ToOxyColor(this Color color)
        {
            return OxyColor.FromArgb(color.A, color.R, color.G, color.B);
        }

        /// <summary>
        /// Converts a <see cref="Brush" /> to an <see cref="OxyColor" />.
        /// </summary>
        /// <param name="brush">The brush.</param>
        /// <returns>An <see cref="OxyColor" />.</returns>
        public static OxyColor ToOxyColor(this IBrush brush)
        {
            if (brush is ImmutableSolidColorBrush iscb)
            {
                return iscb.Color.ToOxyColor();
            }
            else if (brush is SolidColorBrush scb)
            {
                return scb.Color.ToOxyColor();
            }
            return OxyColors.Undefined;
        }

        /// <summary>
        /// Converts a Thickness to an <see cref="OxyThickness" />.
        /// </summary>
        /// <param name="t">The thickness.</param>
        /// <returns>An <see cref="OxyThickness" />.</returns>
        public static OxyThickness ToOxyThickness(this Thickness t)
        {
            return new OxyThickness(t.Left, t.Top, t.Right, t.Bottom);
        }

        /// <summary>
        /// Converts a <see cref="Point" /> to a <see cref="ScreenPoint" />.
        /// </summary>
        /// <param name="pt">The point.</param>
        /// <returns>A <see cref="ScreenPoint" />.</returns>
        public static ScreenPoint ToScreenPoint(this Point pt)
        {
            return new ScreenPoint(pt.X, pt.Y);
        }

        /// <summary>
        /// Converts a Point array to a ScreenPoint array.
        /// </summary>
        /// <param name="points">The points.</param>
        /// <returns>A ScreenPoint array.</returns>
        public static ScreenPoint[] ToScreenPointArray(this Point[] points)
        {
            if (points == null)
            {
                return null;
            }

            var pts = new ScreenPoint[points.Length];
            for (int i = 0; i < points.Length; i++)
            {
                pts[i] = points[i].ToScreenPoint();
            }

            return pts;
        }

        /// <summary>
        /// Converts the specified vector to a ScreenVector.
        /// </summary>
        /// <param name="vector">The vector.</param>
        /// <returns>A <see cref="ScreenVector" />.</returns>
        public static ScreenVector ToScreenVector(this Vector vector)
        {
            return new ScreenVector(vector.X, vector.Y);
        }

        /// <summary>
        /// Converts the specified key.
        /// </summary>
        /// <param name="k">The key to convert.</param>
        /// <returns>The converted key.</returns>
        public static OxyKey Convert(this Key k)
        {
            switch (k)
            {
                case Key.A:
                    return OxyKey.A;
                case Key.Add:
                    return OxyKey.Add;
                case Key.B:
                    return OxyKey.B;
                case Key.Back:
                    return OxyKey.Backspace;
                case Key.C:
                    return OxyKey.C;
                case Key.D:
                    return OxyKey.D;
                case Key.D0:
                    return OxyKey.D0;
                case Key.D1:
                    return OxyKey.D1;
                case Key.D2:
                    return OxyKey.D2;
                case Key.D3:
                    return OxyKey.D3;
                case Key.D4:
                    return OxyKey.D4;
                case Key.D5:
                    return OxyKey.D5;
                case Key.D6:
                    return OxyKey.D6;
                case Key.D7:
                    return OxyKey.D7;
                case Key.D8:
                    return OxyKey.D8;
                case Key.D9:
                    return OxyKey.D9;
                case Key.Decimal:
                    return OxyKey.Decimal;
                case Key.Delete:
                    return OxyKey.Delete;
                case Key.Divide:
                    return OxyKey.Divide;
                case Key.Down:
                    return OxyKey.Down;
                case Key.E:
                    return OxyKey.E;
                case Key.End:
                    return OxyKey.End;
                case Key.Enter:
                    return OxyKey.Enter;
                case Key.Escape:
                    return OxyKey.Escape;
                case Key.F:
                    return OxyKey.F;
                case Key.F1:
                    return OxyKey.F1;
                case Key.F10:
                    return OxyKey.F10;
                case Key.F11:
                    return OxyKey.F11;
                case Key.F12:
                    return OxyKey.F12;
                case Key.F2:
                    return OxyKey.F2;
                case Key.F3:
                    return OxyKey.F3;
                case Key.F4:
                    return OxyKey.F4;
                case Key.F5:
                    return OxyKey.F5;
                case Key.F6:
                    return OxyKey.F6;
                case Key.F7:
                    return OxyKey.F7;
                case Key.F8:
                    return OxyKey.F8;
                case Key.F9:
                    return OxyKey.F9;
                case Key.G:
                    return OxyKey.G;
                case Key.H:
                    return OxyKey.H;
                case Key.Home:
                    return OxyKey.Home;
                case Key.I:
                    return OxyKey.I;
                case Key.Insert:
                    return OxyKey.Insert;
                case Key.J:
                    return OxyKey.J;
                case Key.K:
                    return OxyKey.K;
                case Key.L:
                    return OxyKey.L;
                case Key.Left:
                    return OxyKey.Left;
                case Key.M:
                    return OxyKey.M;
                case Key.Multiply:
                    return OxyKey.Multiply;
                case Key.N:
                    return OxyKey.N;
                case Key.NumPad0:
                    return OxyKey.NumPad0;
                case Key.NumPad1:
                    return OxyKey.NumPad1;
                case Key.NumPad2:
                    return OxyKey.NumPad2;
                case Key.NumPad3:
                    return OxyKey.NumPad3;
                case Key.NumPad4:
                    return OxyKey.NumPad4;
                case Key.NumPad5:
                    return OxyKey.NumPad5;
                case Key.NumPad6:
                    return OxyKey.NumPad6;
                case Key.NumPad7:
                    return OxyKey.NumPad7;
                case Key.NumPad8:
                    return OxyKey.NumPad8;
                case Key.NumPad9:
                    return OxyKey.NumPad9;
                case Key.O:
                    return OxyKey.O;
                case Key.P:
                    return OxyKey.P;
                case Key.PageDown:
                    return OxyKey.PageDown;
                case Key.PageUp:
                    return OxyKey.PageUp;
                case Key.Q:
                    return OxyKey.Q;
                case Key.R:
                    return OxyKey.R;
                case Key.Right:
                    return OxyKey.Right;
                case Key.S:
                    return OxyKey.S;
                case Key.Space:
                    return OxyKey.Space;
                case Key.Subtract:
                    return OxyKey.Subtract;
                case Key.T:
                    return OxyKey.T;
                case Key.Tab:
                    return OxyKey.Tab;
                case Key.U:
                    return OxyKey.U;
                case Key.Up:
                    return OxyKey.Up;
                case Key.V:
                    return OxyKey.V;
                case Key.W:
                    return OxyKey.W;
                case Key.X:
                    return OxyKey.X;
                case Key.Y:
                    return OxyKey.Y;
                case Key.Z:
                    return OxyKey.Z;
                default:
                    return OxyKey.Unknown;
            }
        }

        /// <summary>
        /// Converts the specified button.
        /// </summary>
        /// <param name="button">The button to convert.</param>
        /// <returns>The converted mouse button.</returns>
        [Obsolete]
        public static OxyMouseButton Convert(this MouseButton button)
        {
            switch (button)
            {
                case MouseButton.Left:
                    return OxyMouseButton.Left;
                case MouseButton.Middle:
                    return OxyMouseButton.Middle;
                case MouseButton.Right:
                    return OxyMouseButton.Right;
                default:
                    return OxyMouseButton.None;
            }
        }

        public static OxyMouseButton Convert(this PointerUpdateKind pointerUpdateKind)
        {
            switch (pointerUpdateKind)
            {
                case PointerUpdateKind.LeftButtonPressed:
                    return OxyMouseButton.Left;
                case PointerUpdateKind.MiddleButtonPressed:
                    return OxyMouseButton.Middle;
                case PointerUpdateKind.RightButtonPressed:
                    return OxyMouseButton.Right;
                case PointerUpdateKind.XButton1Pressed:
                    return OxyMouseButton.XButton1;
                case PointerUpdateKind.XButton2Pressed:
                    return OxyMouseButton.XButton2;
                default:
                    return OxyMouseButton.None;
            }
        }

        public static OxyModifierKeys ToModifierKeys(this KeyModifiers modifiers)
        {
            var modifierKeys = OxyModifierKeys.None;
            if ((modifiers & KeyModifiers.Shift) != 0)
            {
                modifierKeys |= OxyModifierKeys.Shift;
            }

            if ((modifiers & KeyModifiers.Control) != 0)
            {
                modifierKeys |= OxyModifierKeys.Control;
            }

            if ((modifiers & KeyModifiers.Alt) != 0)
            {
                modifierKeys |= OxyModifierKeys.Alt;
            }

            if ((modifiers & KeyModifiers.Meta) != 0)
            {
                modifierKeys |= OxyModifierKeys.Windows;
            }

            return modifierKeys;
        }

        /// <summary>
        /// Converts <see cref="MouseWheelEventArgs" /> to <see cref="OxyMouseWheelEventArgs" /> for a mouse wheel event.
        /// </summary>
        /// <param name="e">The <see cref="MouseWheelEventArgs" /> instance containing the event data.</param>
        /// <param name="relativeTo">The <see cref="IInputElement" /> that the event is relative to.</param>
        /// <returns>A <see cref="OxyMouseWheelEventArgs" /> containing the converted event arguments.</returns>
        public static OxyMouseWheelEventArgs ToMouseWheelEventArgs(this PointerWheelEventArgs e, IInputElement relativeTo)
        {
            return new OxyMouseWheelEventArgs
            {
                Position = e.GetPosition(relativeTo as Visual).ToScreenPoint(),
                ModifierKeys = e.KeyModifiers.ToModifierKeys(),
                Delta = (int)(e.Delta.Y + e.Delta.X) * 120
            };
        }

        /// <summary>
        /// Converts <see cref="MouseButtonEventArgs" /> to <see cref="OxyMouseEventArgs" /> for a mouse down event.
        /// </summary>
        /// <param name="e">The <see cref="MouseButtonEventArgs" /> instance containing the event data.</param>
        /// <param name="relativeTo">The <see cref="IInputElement" /> that the event is relative to.</param>
        /// <returns>A <see cref="OxyMouseEventArgs" /> containing the converted event arguments.</returns>
        public static OxyMouseDownEventArgs ToMouseDownEventArgs(this PointerPressedEventArgs e, IInputElement relativeTo)
        {
            var point = e.GetCurrentPoint(relativeTo as Visual);

            return new OxyMouseDownEventArgs
            {
                ChangedButton = point.Properties.PointerUpdateKind.Convert(),
#pragma warning disable CS0618 // Type or member is obsolete
                ClickCount = e.ClickCount,
#pragma warning restore CS0618 // Type or member is obsolete
                Position = e.GetPosition(relativeTo  as Visual).ToScreenPoint(),
                ModifierKeys = e.KeyModifiers.ToModifierKeys()
            };
        }

        /// <summary>
        /// Converts <see cref="MouseButtonEventArgs" /> to <see cref="OxyMouseEventArgs" /> for a mouse up event.
        /// </summary>
        /// <param name="e">The <see cref="MouseButtonEventArgs" /> instance containing the event data.</param>
        /// <param name="relativeTo">The <see cref="IInputElement" /> that the event is relative to.</param>
        /// <returns>A <see cref="OxyMouseEventArgs" /> containing the converted event arguments.</returns>
        public static OxyMouseEventArgs ToMouseReleasedEventArgs(this PointerReleasedEventArgs e, IInputElement relativeTo)
        {
            return new OxyMouseEventArgs
            {
                Position = e.GetPosition(relativeTo as Visual).ToScreenPoint(),
                ModifierKeys = e.KeyModifiers.ToModifierKeys()
            };
        }

        /// <summary>
        /// Converts <see cref="MouseEventArgs" /> to <see cref="OxyMouseEventArgs" /> for a mouse event.
        /// </summary>
        /// <param name="e">The <see cref="MouseEventArgs" /> instance containing the event data.</param>
        /// <param name="relativeTo">The <see cref="IInputElement" /> that the event is relative to.</param>
        /// <returns>A <see cref="OxyMouseEventArgs" /> containing the converted event arguments.</returns>
        public static OxyMouseEventArgs ToMouseEventArgs(this PointerEventArgs e, IInputElement relativeTo)
        {
            return new OxyMouseEventArgs
            {
                Position = e.GetPosition(relativeTo  as Visual).ToScreenPoint(),
                ModifierKeys = e.KeyModifiers.ToModifierKeys()
            };
        }

        /** Touch Events currently not supported in Avalonia
            /// <summary>
            /// Converts <see cref="ManipulationStartedEventArgs" /> to <see cref="OxyMouseEventArgs" /> for a touch started event.
            /// </summary>
            /// <param name="e">The <see cref="ManipulationStartedEventArgs" /> instance containing the event data.</param>
            /// <param name="relativeTo">The <see cref="UIElement" /> that the event is relative to.</param>
            /// <returns>A <see cref="OxyMouseEventArgs" /> containing the converted event arguments.</returns>
            public static OxyTouchEventArgs ToTouchEventArgs(this ManipulationStartedEventArgs e, UIElement relativeTo)
            {
                return new OxyTouchEventArgs
                {
                    Position = e.ManipulationOrigin.ToScreenPoint(),
                };
            }

            /// <summary>
            /// Converts <see cref="ManipulationDeltaEventArgs" /> to <see cref="OxyMouseEventArgs" /> for a touch delta event.
            /// </summary>
            /// <param name="e">The <see cref="ManipulationDeltaEventArgs" /> instance containing the event data.</param>
            /// <param name="relativeTo">The <see cref="UIElement" /> that the event is relative to.</param>
            /// <returns>A <see cref="OxyMouseEventArgs" /> containing the converted event arguments.</returns>
            public static OxyTouchEventArgs ToTouchEventArgs(this ManipulationDeltaEventArgs e, UIElement relativeTo)
            {
                return new OxyTouchEventArgs
                {
                    Position = e.ManipulationOrigin.ToScreenPoint(),
                    DeltaTranslation = e.DeltaManipulation.Translation.ToScreenVector(),
                    DeltaScale = e.DeltaManipulation.Scale.ToScreenVector()
                };
            }

            /// <summary>
            /// Converts <see cref="ManipulationCompletedEventArgs" /> to <see cref="OxyMouseEventArgs" /> for a touch completed event.
            /// </summary>
            /// <param name="e">The <see cref="ManipulationCompletedEventArgs" /> instance containing the event data.</param>
            /// <param name="relativeTo">The <see cref="UIElement" /> that the event is relative to.</param>
            /// <returns>A <see cref="OxyMouseEventArgs" /> containing the converted event arguments.</returns>
            public static OxyTouchEventArgs ToTouchEventArgs(this ManipulationCompletedEventArgs e, UIElement relativeTo)
            {
                return new OxyTouchEventArgs
                {
                    Position = e.ManipulationOrigin.ToScreenPoint()
                };
            }

        */
    }
}
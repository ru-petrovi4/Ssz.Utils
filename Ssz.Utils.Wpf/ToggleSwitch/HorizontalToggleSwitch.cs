﻿using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;

namespace Ssz.Utils.Wpf.ToggleSwitch
{
    ///<summary>
    /// Horizontally oriented toggle switch control.
    ///</summary>
    public class HorizontalToggleSwitch : ToggleSwitchBase
    {
        public HorizontalToggleSwitch()
        {
            DefaultStyleKey = typeof(HorizontalToggleSwitch);
        }

        protected override double Offset
        {
            get { return Canvas.GetLeft(SwitchThumb); }
            set
            {
                SwitchTrack!.BeginAnimation(Canvas.LeftProperty, null);
                SwitchThumb!.BeginAnimation(Canvas.LeftProperty, null);
                Canvas.SetLeft(SwitchTrack, value);
                Canvas.SetLeft(SwitchThumb, value);
            }
        }

        protected override PropertyPath SlidePropertyPath
        {
            get { return new PropertyPath("(Canvas.Left)"); }
        }

        protected override void OnDragDelta(object? sender, DragDeltaEventArgs e)
        {
            DragOffset += e.HorizontalChange;
            Offset = Math.Max(UncheckedOffset, Math.Min(CheckedOffset, DragOffset));
        }

        protected override void LayoutControls()
        {
            if (SwitchThumb is null || SwitchRoot is null)
            {
                return;
            }

            double fullThumbWidth = SwitchThumb.ActualWidth + SwitchThumb.BorderThickness.Left + SwitchThumb.BorderThickness.Right;

            if (SwitchChecked is not null && SwitchUnchecked is not null)
            {
                SwitchChecked.Width = SwitchUnchecked.Width = Math.Max(0, SwitchRoot.ActualWidth - fullThumbWidth / 2);
                SwitchChecked.Padding = new Thickness(0, 0, (SwitchThumb.ActualWidth + SwitchThumb.BorderThickness.Left) / 2, 0);
                SwitchUnchecked.Padding = new Thickness((SwitchThumb.ActualWidth + SwitchThumb.BorderThickness.Right) / 2, 0, 0, 0);
            }

            SwitchThumb.Margin = new Thickness(SwitchRoot.ActualWidth - fullThumbWidth, SwitchThumb.Margin.Top, 0, SwitchThumb.Margin.Bottom);
            UncheckedOffset = -SwitchRoot.ActualWidth + fullThumbWidth - SwitchThumb.BorderThickness.Left;
            CheckedOffset = SwitchThumb.BorderThickness.Right;

            if (!IsDragging)
            {
                Offset = IsChecked ? CheckedOffset : UncheckedOffset;
                ChangeCheckStates(false);
            }
        }

        protected override void OnDragCompleted(object? sender, DragCompletedEventArgs e)
        {
            IsDragging = false;
            bool click = false;
            double fullThumbWidth = SwitchThumb!.ActualWidth + SwitchThumb.BorderThickness.Left + SwitchThumb.BorderThickness.Right;

            if ((!IsChecked && DragOffset > (SwitchRoot!.ActualWidth - fullThumbWidth) * (Elasticity - 1.0))
                 || (IsChecked && DragOffset < (SwitchRoot!.ActualWidth - fullThumbWidth) * -Elasticity))
            {
                double edge = IsChecked ? CheckedOffset : UncheckedOffset;
                if (Offset != edge)
                {
                    click = true;
                }
            }
            else if (DragOffset == CheckedOffset || DragOffset == UncheckedOffset)
            {
                click = true;
            }
            else
            {
                ChangeCheckStates(true);
            }

            if (click)
            {
                OnClick();
            }

            DragOffset = 0;
        }
    }
}
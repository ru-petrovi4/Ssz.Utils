﻿// ReSharper disable once CheckNamespace
namespace Fluent
{
    using System.Windows;
    using Fluent.Internal.KnownBoxes;

    /// <summary>
    /// Represents ribbon status bar item
    /// </summary>
    public class StatusBarItem : System.Windows.Controls.Primitives.StatusBarItem
    {
        #region Properties

        #region Title

        /// <summary>
        /// Gets or sets ribbon status bar item
        /// </summary>
        public string? Title
        {
            get { return (string?)this.GetValue(TitleProperty); }
            set { this.SetValue(TitleProperty, value); }
        }

        /// <summary>Identifies the <see cref="Title"/> dependency property.</summary>
        public static readonly DependencyProperty TitleProperty =
            DependencyProperty.Register(nameof(Title), typeof(string), typeof(StatusBarItem), new PropertyMetadata());

        #endregion

        #region Value

        /// <summary>
        /// Gets or sets ribbon status bar value
        /// </summary>
        public string? Value
        {
            get { return (string?)this.GetValue(ValueProperty); }
            set { this.SetValue(ValueProperty, value); }
        }

        /// <summary>Identifies the <see cref="Value"/> dependency property.</summary>
        public static readonly DependencyProperty ValueProperty =
            DependencyProperty.Register(nameof(Value), typeof(string), typeof(StatusBarItem),
            new PropertyMetadata(OnValueChanged));

        private static void OnValueChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var item = (StatusBarItem)d;
            item.CoerceValue(ContentProperty);
        }

        #endregion

        #region isChecked

        /// <summary>
        /// Gets or sets whether status bar item is checked in menu
        /// </summary>
        public bool IsChecked
        {
            get { return (bool)this.GetValue(IsCheckedProperty); }
            set { this.SetValue(IsCheckedProperty, BooleanBoxes.Box(value)); }
        }

        /// <summary>Identifies the <see cref="IsChecked"/> dependency property.</summary>
        public static readonly DependencyProperty IsCheckedProperty =
            DependencyProperty.Register(nameof(IsChecked), typeof(bool), typeof(StatusBarItem), new PropertyMetadata(BooleanBoxes.TrueBox, OnIsCheckedChanged));

        // Handles IsChecked changed
        private static void OnIsCheckedChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var item = (StatusBarItem)d;
            item.CoerceValue(VisibilityProperty);

            if ((bool)e.NewValue)
            {
                item.RaiseChecked();
            }
            else
            {
                item.RaiseUnchecked();
            }
        }

        #endregion

        #endregion

        #region Events

        /// <summary>
        /// Occurs when status bar item checks
        /// </summary>
        public event RoutedEventHandler? Checked;

        /// <summary>
        /// Occurs when status bar item unchecks
        /// </summary>
        public event RoutedEventHandler? Unchecked;

        // Raises checked event
#pragma warning disable WPF0005 // Name of PropertyChangedCallback should match registered name.
        private void RaiseChecked()
        {
            this.Checked?.Invoke(this, new RoutedEventArgs());
        }

        // Raises unchecked event
        private void RaiseUnchecked()
        {
            this.Unchecked?.Invoke(this, new RoutedEventArgs());
        }
#pragma warning restore WPF0005 // Name of PropertyChangedCallback should match registered name.

        #endregion

        #region Constructors

        /// <summary>
        /// Static constructor
        /// </summary>
        static StatusBarItem()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(StatusBarItem), new FrameworkPropertyMetadata(typeof(StatusBarItem)));
            VisibilityProperty.AddOwner(typeof(StatusBarItem), new FrameworkPropertyMetadata(null, CoerceVisibility));
            ContentProperty.AddOwner(typeof(StatusBarItem), new FrameworkPropertyMetadata(OnContentChanged, CoerceContent));
        }

        // Content changing handler
        private static void OnContentChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var item = (StatusBarItem)d;
            item.CoerceValue(ValueProperty);
        }

        // Coerce content
        private static object? CoerceContent(DependencyObject d, object? basevalue)
        {
            var item = (StatusBarItem)d;
            // if content is null returns value
            if (basevalue is null
                && item.Value is not null)
            {
                return item.Value;
            }

            return basevalue;
        }

        // Coerce visibility
        private static object? CoerceVisibility(DependencyObject d, object? basevalue)
        {
            // If unchecked when not visible in status bar
            if (((StatusBarItem)d).IsChecked == false)
            {
                return Visibility.Collapsed;
            }

            return basevalue;
        }

        #endregion
    }
}

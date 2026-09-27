using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;

namespace Ssz.Operator.Core.VisualEditors.Windows
{
    /// <summary>
    ///     What every editor dialog puts its own control inside: a padded frame, plus Ctrl+Enter for a
    ///     new line in whatever text box has the focus.
    ///     <para>
    ///         Ported from the WPF editor. There the shortcut was an InputGesture of a RoutedCommand;
    ///         here the frame listens for the keys itself, which is the same thing without the routing.
    ///         The button that opened the Windows character map is gone: it is a Windows program, and
    ///         the editor now also runs in a browser.
    ///     </para>
    /// </summary>
    public class EditorFrameControl : UserControl
    {
        #region construction and destruction

        public EditorFrameControl()
        {
            _mainContentControl = new Border
            {
                Name = @"MainContentControl",
                BorderThickness = new Thickness(0),
                Padding = new Thickness(5)
            };

            var grid = new Grid { RowDefinitions = new RowDefinitions(@"Auto,*") };
            Grid.SetRow(_mainContentControl, 1);
            grid.Children.Add(_mainContentControl);

            Content = grid;

            AddHandler(KeyDownEvent, OnPreviewKeyDown, Avalonia.Interactivity.RoutingStrategies.Tunnel);
        }

        #endregion

        #region public functions

        public Control? MainContent
        {
            get => _mainContentControl.Child;
            set => _mainContentControl.Child = value;
        }

        #endregion

        #region private functions

        private void OnPreviewKeyDown(object? sender, KeyEventArgs e)
        {
            if (e.Key != Key.Enter || !e.KeyModifiers.HasFlag(KeyModifiers.Control)) return;

            if (TopLevel.GetTopLevel(this)?.FocusManager?.GetFocusedElement() is not TextBox textBox) return;
            if (textBox.IsReadOnly) return;

            var caretIndex = textBox.CaretIndex;
            var text = textBox.Text ?? String.Empty;
            caretIndex = Math.Clamp(caretIndex, 0, text.Length);

            textBox.Text = text.Insert(caretIndex, Environment.NewLine);
            textBox.CaretIndex = caretIndex + Environment.NewLine.Length;

            e.Handled = true;
        }

        #endregion

        #region private fields

        private readonly Border _mainContentControl;

        #endregion
    }
}

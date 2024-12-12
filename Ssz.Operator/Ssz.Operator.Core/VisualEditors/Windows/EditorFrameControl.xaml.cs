using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Microsoft.Extensions.Logging;
using Ssz.Operator.Core.Utils;

namespace Ssz.Operator.Core.VisualEditors.Windows
{
    public partial class EditorFrameControl : UserControl
    {
        #region construction and destruction

        public EditorFrameControl()
        {
            InitializeComponent();

            CommandBindings.Add(new CommandBinding(InsertNewLineRoutedCommand, InsertNewLineExecuted));
            InsertNewLineRoutedCommand.InputGestures.Add(new KeyGesture(Key.Enter, ModifierKeys.Control));
        }

        #endregion

        #region public functions

        public readonly RoutedCommand InsertNewLineRoutedCommand = new();

        public UIElement MainContent
        {
            get => MainContentControl.Child;
            set => MainContentControl.Child = value;
        }

        #endregion

        #region private functions

        private void InsertNewLineExecuted(object? sender, ExecutedRoutedEventArgs e)
        {
            var textBox = Keyboard.FocusedElement as TextBox;
            if (textBox is not null && !textBox.IsReadOnly) textBox.InsertNewLine();
        }

        private void InsertSpecialCharacterButtonOnClicked(object? sender, RoutedEventArgs e)
        {
            try
            {
                using (var p = new Process())
                {
                    p.StartInfo.FileName = "charmap";
                    p.Start();
                }
            }
            catch (Exception ex)
            {
                DsProject.LoggersSet.Logger.LogError(ex, "Starting charmap failed");
            }
        }

        #endregion
    }
}
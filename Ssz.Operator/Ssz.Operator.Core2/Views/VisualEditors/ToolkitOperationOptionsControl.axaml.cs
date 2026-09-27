using System;
using Avalonia.Controls;
using Avalonia.Interactivity;

namespace Ssz.Operator.Core.VisualEditors
{
    /// <summary>
    ///     What a toolkit operation is to be run with, and the say to run it or not.
    /// </summary>
    public partial class ToolkitOperationOptionsControl : UserControl
    {
        #region construction and destruction

        public ToolkitOperationOptionsControl()
        {
            InitializeComponent();
        }

        #endregion

        #region public functions

        public object? SelectedObject
        {
            get => ObjectPropertiesControl.SelectedObject;
            set => ObjectPropertiesControl.SelectedObject = value;
        }

        public string? Description
        {
            get => DescriptionTextBlock.Text;
            set => DescriptionTextBlock.Text = value;
        }

        public event Action? OkEvent;

        public event Action? CancelEvent;

        #endregion

        #region private functions

        private void StartToolkitOperationButtonOnClick(object? sender, RoutedEventArgs e)
        {
            OkEvent?.Invoke();
        }

        private void CancelButtonOnClick(object? sender, RoutedEventArgs e)
        {
            CancelEvent?.Invoke();
        }

        #endregion
    }
}

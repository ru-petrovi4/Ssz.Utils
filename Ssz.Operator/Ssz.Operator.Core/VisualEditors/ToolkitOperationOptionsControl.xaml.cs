using System;
using System.Windows;
using System.Windows.Controls;

namespace Ssz.Operator.Core.VisualEditors
{
    public partial class ToolkitOperationOptionsControl : UserControl
    {
        #region construction and destruction

        public ToolkitOperationOptionsControl()
        {
            InitializeComponent();
        }

        #endregion

        #region private functions

        private void StartToolkitOperationButtonOnClick(object? sender, RoutedEventArgs e)
        {
            var okEvent = OkEvent;
            if (okEvent is not null) okEvent();
        }

        #endregion

        #region public functions

        public object? SelectedObject
        {
            get => ObjectPropertiesControl.SelectedObject;
            set => ObjectPropertiesControl.SelectedObject = value;
        }

        public string Description
        {
            get => DescriptionTextBlock.Text;
            set => DescriptionTextBlock.Text = value;
        }

        public event Action? OkEvent;

        #endregion
    }
}
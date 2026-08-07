using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;

namespace Ssz.Operator.Core.ControlsCommon.Trends
{
    public static class ColorDialogHelper
    {
        #region public functions

        /// <summary>
        ///     Returns null, if user cancelled the dialog or there is no window to own it.
        /// </summary>
        public static async Task<Color?> ShowAsync(Window? owner, Color initialColor)
        {
            owner ??= MessageBoxHelper.GetRootWindow();
            if (owner is null)
                return null;

            return await new ColorDialog(initialColor).ShowDialog<Color?>(owner);
        }

        #endregion
    }

    /// <summary>
    ///     Wraps Avalonia's own ColorView into a modal dialog with OK/Cancel.
    ///     ShowDialog returns the picked color, or null when cancelled.
    /// </summary>
    internal sealed class ColorDialog : Window
    {
        #region construction and destruction

        public ColorDialog(Color initialColor)
        {
            Title = Properties.OperatorUIResources.TrendColorDialogTitle;
            SizeToContent = SizeToContent.WidthAndHeight;
            CanResize = false;
            ShowInTaskbar = false;
            WindowStartupLocation = WindowStartupLocation.CenterOwner;

            _colorView = new ColorView
            {
                Color = initialColor,
                IsAlphaEnabled = true,
                IsAlphaVisible = true
            };

            var okButton = new Button
            {
                Content = Properties.Resources.OkButtonText,
                IsDefault = true,
                MinWidth = 80
            };
            okButton.Click += (_, _) => Close(_colorView.Color);

            var cancelButton = new Button
            {
                Content = Properties.Resources.CancelButtonText,
                IsCancel = true,
                MinWidth = 80
            };
            cancelButton.Click += (_, _) => Close(null);

            var buttonsPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Right,
                Spacing = 8,
                Margin = new Thickness(0, 12, 0, 0)
            };
            buttonsPanel.Children.Add(okButton);
            buttonsPanel.Children.Add(cancelButton);

            var rootPanel = new StackPanel
            {
                Margin = new Thickness(12)
            };
            rootPanel.Children.Add(_colorView);
            rootPanel.Children.Add(buttonsPanel);

            Content = rootPanel;
        }

        #endregion

        #region private fields

        private readonly ColorView _colorView;

        #endregion
    }
}

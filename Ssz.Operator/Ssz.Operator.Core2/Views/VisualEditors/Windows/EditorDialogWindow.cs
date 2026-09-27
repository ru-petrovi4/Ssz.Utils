using System;
using Avalonia.Controls;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Ssz.Operator.Core.ControlsCommon;

namespace Ssz.Operator.Core.VisualEditors.Windows
{
    /// <summary>
    ///     What every editor of the property grid opens: a window remembering where it was last put,
    ///     holding one editing control.
    ///     <para>
    ///         Ported from the WPF dialogs, which all shared this shape. They also all accepted on
    ///         close rather than asking - there was no Cancel - and that is kept, so that the author
    ///         closing the window means what it meant before.
    ///     </para>
    /// </summary>
    public class EditorDialogWindow : LocationMindfulWindow
    {
        #region construction and destruction

        /// <summary>
        ///     For using in the previewer.
        /// </summary>
        public EditorDialogWindow()
        {
        }

        public EditorDialogWindow(string category, double initialWidth = Double.NaN,
            double initialHeight = Double.NaN)
            : base(category, initialWidth, initialHeight)
        {
            Icon = DialogIcon;

            Content = new EditorFrameControl();
        }

        #endregion

        #region public functions

        /// <summary>
        ///     Whether the author accepted what the dialog holds. Closing the window accepts, as it did
        ///     in the WPF editor.
        /// </summary>
        public bool DialogResult { get; protected set; }

        #endregion

        #region protected functions

        protected EditorFrameControl Frame => (EditorFrameControl) Content!;

        protected override void OnClosing(WindowClosingEventArgs e)
        {
            base.OnClosing(e);

            DialogResult = true;
        }

        #endregion

        #region private functions

        private static WindowIcon? DialogIcon
        {
            get
            {
                if (_dialogIcon is not null) return _dialogIcon;

                try
                {
                    _dialogIcon = new WindowIcon(new Bitmap(AssetLoader.Open(
                        new Uri(@"avares://Ssz.Operator.Core/Resources/Images/Properties.png"))));
                }
                catch (Exception)
                {
                    // A window without its icon is still a window.
                }

                return _dialogIcon;
            }
        }

        #endregion

        #region private fields

        private static WindowIcon? _dialogIcon;

        #endregion
    }
}

using Avalonia.Controls;

namespace Ssz.Operator.Core.ControlsPlay.VirtualKeyboards
{
    public partial class VirtualKeyboardWindow : Window, IVirtualKeyboardWindow
    {
        #region construction and destruction

        public VirtualKeyboardWindow(Control keyboardView, string virtualKeyboardType)
        {
            InitializeComponent();

            Main.Child = keyboardView;
            VirtualKeyboardType = virtualKeyboardType;

            Closed += (_, _) => { PlayDsProjectView.VirtualKeyboardWindow = null; };
            PlayDsProjectView.VirtualKeyboardWindow = this;

            // ---------------------------------------------------------------
            // CROSS-PLATFORM WIRING:
            // Pass THIS window as the TopLevel to AvaloniaKeyboardSender so it
            // can inject events into the correct window.
            //
            // If keyboardView.DataContext is a GenericKeyboardViewModel whose
            // GenericKeyboardModel.Sender is an AvaloniaKeyboardSender with
            // targetTopLevel == null, replace it now that we have the TopLevel.
            // ---------------------------------------------------------------
            if (keyboardView.DataContext is GenericKeyboardViewModel vm &&
                vm.KeyboardModel.Sender is AvaloniaKeyboardSender sender &&
                sender.TargetTopLevel is null)
            {
                // Get the OWNER window (the one the user is typing into),
                // not this keyboard window itself.
                // PlayDsProjectView.MainWindow is the target — adjust to your API.
                var targetWindow = PlayDsProjectView.MainWindow as TopLevel
                                   ?? TopLevel.GetTopLevel(keyboardView);
                sender.SetTargetTopLevel(targetWindow);
            }
        }

        #endregion

        #region public functions

        public string VirtualKeyboardType { get; }

        #endregion
    }
}

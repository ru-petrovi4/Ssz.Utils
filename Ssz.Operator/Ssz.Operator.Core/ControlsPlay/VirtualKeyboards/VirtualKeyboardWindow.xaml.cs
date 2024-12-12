using System;
using System.Windows;

namespace Ssz.Operator.Core.ControlsPlay.VirtualKeyboards
{
    public partial class VirtualKeyboardWindow : TouchScreenWindowBase, IVirtualKeyboardWindow
    {
        #region construction and destruction

        public VirtualKeyboardWindow(UIElement keyboardView, string virtualKeyboardType)
        {
            InitializeComponent();

            Main.Child = keyboardView;
            VirtualKeyboardType = virtualKeyboardType;

            //If the keyboard window is closed, set the Ssz.Operator.Play framework window back to null
            Closed += (sender, args) => { PlayDsProjectView.VirtualKeyboardWindow = null; };

            //Tell the Ssz.Operator.Play Framework which Virtual Keyboard is currently active.
            PlayDsProjectView.VirtualKeyboardWindow = this;

            Title = "Virtual Keyboard";
        }

        #endregion

        #region public functions

        public string VirtualKeyboardType { get; }

        #endregion
    }
}
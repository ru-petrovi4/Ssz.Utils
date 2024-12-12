using System;
using System.Windows;
using Ssz.Operator.Core.ControlsCommon;

namespace Ssz.Operator.Core.ControlsPlay.PanoramaPlay.Map3D
{
    public partial class Map3DWindow : LocationMindfulWindow
    {
        #region construction and destruction

        public Map3DWindow() : base(@"Map3DWindow", 800, 600)
        {
            InitializeComponent();
        }

        #endregion

        #region private functions

        private static Map3DWindow? Instance { get; set; }

        #endregion

        #region public functions

        public static void ShowAsync()
        {
            Application.Current.Dispatcher.BeginInvoke(new Action(() =>
            {
                if (Instance is not null) Instance.Close();

                var map3DWindow = new Map3DWindow();
                map3DWindow.Owner = MessageBoxHelper.GetRootWindow();
                Instance = map3DWindow;
                map3DWindow.Closed += (sender, args) => Instance = null;
                map3DWindow.Show();
                map3DWindow.Map3DControl.ShowAsync();
            }));
        }

        #endregion
    }
}
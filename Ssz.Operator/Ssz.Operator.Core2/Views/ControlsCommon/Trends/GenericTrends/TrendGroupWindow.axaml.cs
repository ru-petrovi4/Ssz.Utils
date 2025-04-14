using System;
using Avalonia.Controls;
using Ssz.Operator.Core.ControlsCommon;

namespace Ssz.Operator.Core.ControlsCommon.Trends.GenericTrends
{    
    public partial class TrendGroupWindow : Window
    {
        #region construction and destruction

        protected TrendGroupWindow()
            //base("Generic.TrendGroupWindow", 1300, 800)
        {
            InitializeComponent();
        }

        #endregion

        #region public functions

        public static void ShowOrActivate(WindowType windowType, string groupId)
        {
            if (_instance == null)
            {
                _instance = new TrendGroupWindow();                
                _instance.Show();
            }
            else
            {
                _instance.Activate();
            }

            _instance.Jump(windowType, groupId);
        }        

        #endregion

        #region protected functions

        protected override void OnClosed(EventArgs e)
        {
            base.OnClosed(e);

            (Content as IDisposable)?.Dispose();

            _instance = null;
        }

        #endregion

        #region private functions

        private void Jump(WindowType windowType, string param_)
        {
            (Content as IDisposable)?.Dispose();

            switch (windowType)
            {
                case WindowType.TrendGroup:
                    {
                        var userTrendGroupControl = new TrendGroupControl();
                        userTrendGroupControl.Jump(param_, @"");
                        Content = userTrendGroupControl;
                    }
                    break;
                case WindowType.UserTrendGroup:
                    {
                        var userTrendGroupControl = new TrendGroupControl();
                        userTrendGroupControl.Jump(param_, @"");
                        Content = userTrendGroupControl;
                    }
                    break;
                case WindowType.TrendForTag:
                    {
                        var userTrendGroupControl = new TrendGroupControl();
                        userTrendGroupControl.Jump(@"", param_);
                        Content = userTrendGroupControl;
                    }
                    break;
            }            
        }

        #endregion

        #region private fields

        private static TrendGroupWindow? _instance;

        #endregion

        public enum WindowType
        {            
            UserTrendGroup,
            TrendGroup,
            TrendForTag,
        }
    }
}
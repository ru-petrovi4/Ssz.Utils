using System;
using Avalonia.Controls;
using Ssz.Operator.Core.ControlsCommon;

namespace Ssz.Operator.Core.ControlsCommon.Trends.GenericTrends
{
    public partial class TrendGroupWindow : Window
    {
        #region construction and destruction

        public TrendGroupWindow()
        {
            InitializeComponent();
        }

        #endregion

        #region public functions

        public static void ShowOrActivate(WindowType windowType, string groupId)
        {
            if (_instance is null)
            {
                _instance = new TrendGroupWindow();
                try
                {
                    _instance.Owner = MessageBoxHelper.GetRootWindow();
                }
                catch
                {
                }
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

            Title = param_;

            var trendGroupControl = new TrendGroupControl();
            switch (windowType)
            {
                case WindowType.TrendGroup:
                case WindowType.UserTrendGroup:
                    trendGroupControl.Jump(param_, @"");
                    break;
                case WindowType.TrendForTag:
                    trendGroupControl.Jump(@"", param_);
                    break;
            }
            Content = trendGroupControl;
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

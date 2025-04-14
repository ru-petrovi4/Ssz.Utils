using System;
using System.ComponentModel;
using Ssz.Operator.Core.Properties;

namespace Ssz.Operator.Core
{
    public partial class DsProject
    {
        #region private fields

        private bool _designerModeInPlay;

        #endregion

        #region public functions

        public event Action<bool, DesignModeInPlayOnChangingEventArgs>? DesignModeInPlayOnChanging;

        public event Action? DesignModeInPlayChanged;


        [Browsable(false)]
        public bool DesignModeInPlay
        {
            set
            {
                if (value == _designerModeInPlay) return;
                var designerModeInPlayOnChanging = DesignModeInPlayOnChanging;
                var e = new DesignModeInPlayOnChangingEventArgs();
                if (designerModeInPlayOnChanging is not null) designerModeInPlayOnChanging(value, e);
                if (!e.CanHandle)
                {
                    MessageBoxHelper.ShowError(Resources.DesignModeInPlayFailed);
                    return;
                }

                if (e.Cancel) return;
                _designerModeInPlay = value;
                var designerModeInPlayChanged = DesignModeInPlayChanged;
                if (designerModeInPlayChanged is not null) designerModeInPlayChanged();
            }
            get => _designerModeInPlay;
        }

        #endregion
    }

    public class DesignModeInPlayOnChangingEventArgs : EventArgs
    {
        public bool CanHandle { get; set; }

        public bool Cancel { get; set; }
    }
}
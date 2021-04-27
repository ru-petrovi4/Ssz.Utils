/*************************************************************************************
   
   Toolkit for WPF

   Copyright (C) 2007-2018 Ssz.Xceed Software Inc.

   This program is provided to you under the terms of the Microsoft Public
   License (Ms-PL) as published at http://wpftoolkit.codeplex.com/license 

   For more features, controls, and fast professional support,
   pick up the Plus Edition at https://xceed.com/xceed-toolkit-plus-for-wpf/

   Stay informed: follow @datagrid on Twitter or Like http://facebook.com/datagrids

  ***********************************************************************************/

using System;
using System.Diagnostics;
using System.Windows.Threading;
using Ssz.Xceed.Wpf.AvalonDock.Layout;

namespace Ssz.Xceed.Wpf.AvalonDock.Controls
{
    internal class AutoHideWindowManager
    {
        #region Constructors

        internal AutoHideWindowManager(DockingManager manager)
        {
            _manager = manager;
            SetupCloseTimer();
        }

        #endregion

        #region Members

        private readonly DockingManager _manager;
        private WeakReference _currentAutohiddenAnchor;
        private DispatcherTimer _closeTimer;

        #endregion

        #region Private Methods

        public void ShowAutoHideWindow(LayoutAnchorControl anchor)
        {
            if (_currentAutohiddenAnchor.GetValueOrDefault<LayoutAnchorControl>() != anchor)
            {
                StopCloseTimer();
                _currentAutohiddenAnchor = new WeakReference(anchor);
                _manager.AutoHideWindow.Show(anchor);
                StartCloseTimer();
            }
        }

        public void HideAutoWindow(LayoutAnchorControl anchor = null)
        {
            if (anchor == null ||
                anchor == _currentAutohiddenAnchor.GetValueOrDefault<LayoutAnchorControl>())
                StopCloseTimer();
            else
                Debug.Assert(false);
        }

        private void SetupCloseTimer()
        {
            _closeTimer = new DispatcherTimer(DispatcherPriority.Background);
            _closeTimer.Interval = TimeSpan.FromMilliseconds(1500);
            _closeTimer.Tick += (s, e) =>
            {
                if (_manager.AutoHideWindow.IsWin32MouseOver ||
                    ((LayoutAnchorable) _manager.AutoHideWindow.Model).IsActive ||
                    _manager.AutoHideWindow.IsResizing)
                    return;

                StopCloseTimer();
            };
        }

        private void StartCloseTimer()
        {
            _closeTimer.Start();
        }

        private void StopCloseTimer()
        {
            _closeTimer.Stop();
            _manager.AutoHideWindow.Hide();
            _currentAutohiddenAnchor = null;
        }

        #endregion
    }
}
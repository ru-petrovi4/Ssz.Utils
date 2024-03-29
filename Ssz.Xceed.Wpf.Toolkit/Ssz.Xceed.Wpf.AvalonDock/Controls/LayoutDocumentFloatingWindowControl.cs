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
using System.Windows;
using System.Windows.Controls.Primitives;
using Microsoft.Windows.Shell;
using Ssz.Xceed.Wpf.AvalonDock.Layout;

namespace Ssz.Xceed.Wpf.AvalonDock.Controls
{
    public class LayoutDocumentFloatingWindowControl : LayoutFloatingWindowControl
    {
        #region Members

        private readonly LayoutDocumentFloatingWindow _model;

        #endregion

        #region Properties

        public LayoutItem RootDocumentLayoutItem => _model.Root.Manager.GetLayoutItemFromModel(_model.RootDocument);

        #endregion

        #region Constructors

        static LayoutDocumentFloatingWindowControl()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(LayoutDocumentFloatingWindowControl),
                new FrameworkPropertyMetadata(typeof(LayoutDocumentFloatingWindowControl)));
        }

        internal LayoutDocumentFloatingWindowControl(LayoutDocumentFloatingWindow model, bool isContentImmutable)
            : base(model, isContentImmutable)
        {
            _model = model;
            UpdateThemeResources();
        }

        internal LayoutDocumentFloatingWindowControl(LayoutDocumentFloatingWindow model)
            : this(model, false)
        {
        }

        #endregion

        #region Overrides

        public override ILayoutElement Model => _model;

        protected override void OnInitialized(EventArgs e)
        {
            base.OnInitialized(e);

            if (_model.RootDocument is null)
            {
                InternalClose();
            }
            else
            {
                var manager = _model.Root.Manager;

                Content = manager.CreateUIElementForModel(_model.RootDocument);

                _model.RootDocumentChanged += _model_RootDocumentChanged;
            }
        }

        protected override IntPtr FilterMessage(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            switch (msg)
            {
                case Win32Helper.WM_NCLBUTTONDOWN: //Left button down on title -> start dragging over docking manager
                    if (wParam.ToInt32() == Win32Helper.HT_CAPTION)
                        if (_model.RootDocument is not null)
                            _model.RootDocument.IsActive = true;
                    break;
                case Win32Helper.WM_NCRBUTTONUP:
                    if (wParam.ToInt32() == Win32Helper.HT_CAPTION)
                    {
                        if (OpenContextMenu())
                            handled = true;
                        if (_model.Root.Manager.ShowSystemMenu)
                            WindowChrome.GetWindowChrome(this).ShowSystemMenu = !handled;
                        else
                            WindowChrome.GetWindowChrome(this).ShowSystemMenu = false;
                    }

                    break;
            }

            return base.FilterMessage(hwnd, msg, wParam, lParam, ref handled);
        }

        protected override void OnClosed(EventArgs e)
        {
            var root = Model.Root;
            root.Manager.RemoveFloatingWindow(this);
            root.CollectGarbage();

            base.OnClosed(e);

            if (!CloseInitiatedByUser) root.FloatingWindows.Remove(_model);

            _model.RootDocumentChanged -= _model_RootDocumentChanged;
        }

        #endregion

        #region Private Methods

        private void _model_RootDocumentChanged(object sender, EventArgs e)
        {
            if (_model.RootDocument is null) InternalClose();
        }

        private bool OpenContextMenu()
        {
            var ctxMenu = _model.Root.Manager.DocumentContextMenu;
            if (ctxMenu is not null && RootDocumentLayoutItem is not null)
            {
                ctxMenu.PlacementTarget = null;
                ctxMenu.Placement = PlacementMode.MousePoint;
                ctxMenu.DataContext = RootDocumentLayoutItem;
                ctxMenu.IsOpen = true;
                return true;
            }

            return false;
        }

        #endregion
    }
}
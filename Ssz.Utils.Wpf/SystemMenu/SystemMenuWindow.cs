// <copyright file="SystemMenuWindow.cs" company="Nish Sivakumar">
// Copyright (c) Nish Sivakumar. All rights reserved.
// </copyright>

using System;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;

namespace Ssz.Utils.Wpf.SystemMenu
{
    public class SystemMenuWindow : Window
    {
        #region construction and destruction

        /// <summary>
        ///     Initializes a new instance of the SystemMenuWindow class.
        /// </summary>
        public SystemMenuWindow()
        {
            Loaded += SystemMenuWindow_Loaded;

            MenuItems = new FreezableCollection<SystemMenuItem>();
        }

        #endregion

        #region public functions

        public static readonly DependencyProperty MenuItemsProperty = DependencyProperty.Register(
            "MenuItems", typeof (FreezableCollection<SystemMenuItem>), typeof (SystemMenuWindow),
            new PropertyMetadata(new PropertyChangedCallback(OnMenuItemsChanged)));

        public FreezableCollection<SystemMenuItem> MenuItems
        {
            get { return (FreezableCollection<SystemMenuItem>) GetValue(MenuItemsProperty); }

            set { SetValue(MenuItemsProperty, value); }
        }

        #endregion

        #region private functions

        [DllImport("user32.dll")]
        private static extern IntPtr GetSystemMenu(IntPtr hWnd, bool bRevert);

        [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        private static extern bool InsertMenu(IntPtr hmenu, int position, uint flags, uint item_id,
            [MarshalAs(UnmanagedType.LPTStr)] string item_text);

        [DllImport("user32.dll")]
        private static extern bool EnableMenuItem(IntPtr hMenu, uint uIDEnableItem, uint uEnable);

        private static void OnMenuItemsChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var obj = d as SystemMenuWindow;

            if (obj is not null)
            {
                var v = e.NewValue as FreezableCollection<SystemMenuItem>;
                if (v is not null)
                {
                    obj.MenuItems = v;
                }
            }
        }

        private void SystemMenuWindow_Loaded(object? sender, RoutedEventArgs e)
        {
            var interopHelper = new WindowInteropHelper(this);
            systemMenu = GetSystemMenu(interopHelper.Handle, false);

            if (MenuItems.Count > 0)
            {
                InsertMenu(systemMenu, -1, MF_BYPOSITION | MF_SEPARATOR, 0, String.Empty);
            }

            foreach (SystemMenuItem item in MenuItems)
            {
                if (item.IsSeparator)
                {
                    InsertMenu(systemMenu, -1, MF_BYPOSITION | MF_SEPARATOR, 0, String.Empty);
                }
                else
                {
                    InsertMenu(systemMenu, item.Id, MF_BYCOMMAND | MF_STRING, (uint)item.Id, item.Header);
                }
            }

            HwndSource hwndSource = HwndSource.FromHwnd(interopHelper.Handle);
            hwndSource.AddHook(WndProc);
        }

        private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            switch ((uint) msg)
            {
                case WM_SYSCOMMAND:
                    SystemMenuItem? menuItem = MenuItems.FirstOrDefault(mi => mi.Id == wParam.ToInt32());
                    if (menuItem is not null && menuItem.Command is not null)
                    {
                        menuItem.Command.Execute(menuItem.CommandParameter);
                        handled = true;
                    }

                    break;

                case WM_INITMENUPOPUP:
                    if (systemMenu == wParam)
                    {
                        foreach (SystemMenuItem item in MenuItems)
                        {
                            EnableMenuItem(systemMenu, (uint) item.Id,
                                item.Command is not null && item.Command.CanExecute(item.CommandParameter) ? MF_ENABLED : MF_DISABLED);
                        }
                        handled = true;
                    }

                    break;
            }

            return IntPtr.Zero;
        }

        #endregion

        #region private fields

        private IntPtr systemMenu;

        private const uint WM_SYSCOMMAND = 0x112;

        private const uint WM_INITMENUPOPUP = 0x0117;

        private const uint MF_SEPARATOR = 0x800;

        private const uint MF_BYCOMMAND = 0x0;

        private const uint MF_BYPOSITION = 0x400;

        private const uint MF_STRING = 0x0;

        private const uint MF_ENABLED = 0x0;

        private const uint MF_DISABLED = 0x2;

        #endregion
    }
}
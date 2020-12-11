// <copyright file="SystemMenuItem.cs" company="Nish Sivakumar">
// Copyright (c) Nish Sivakumar. All rights reserved.
// </copyright>

using System.Windows;
using System.Windows.Input;

namespace Ssz.Utils.Wpf.SystemMenu
{
    public class SystemMenuItem : Freezable
    {
        #region public functions

        public static readonly DependencyProperty CommandProperty = DependencyProperty.Register(
            "Command", typeof (ICommand), typeof (SystemMenuItem),
            new PropertyMetadata(new PropertyChangedCallback(OnCommandChanged)));

        public static readonly DependencyProperty CommandParameterProperty = DependencyProperty.Register(
            "CommandParameter", typeof (object), typeof (SystemMenuItem));

        public static readonly DependencyProperty HeaderProperty = DependencyProperty.Register(
            "Header", typeof (string), typeof (SystemMenuItem));

        public static readonly DependencyProperty IdProperty = DependencyProperty.Register(
            "Id", typeof (int), typeof (SystemMenuItem));

        public ICommand Command
        {
            get { return (ICommand) GetValue(CommandProperty); }

            set { SetValue(CommandProperty, value); }
        }

        public object CommandParameter
        {
            get { return GetValue(CommandParameterProperty); }

            set { SetValue(CommandParameterProperty, value); }
        }

        public string Header
        {
            get { return (string) GetValue(HeaderProperty); }

            set { SetValue(HeaderProperty, value); }
        }

        public int Id
        {
            get { return (int) GetValue(IdProperty); }

            set { SetValue(IdProperty, value); }
        }

        public bool IsSeparator { get; set; }

        #endregion

        #region protected functions

        protected override Freezable CreateInstanceCore()
        {
            return new SystemMenuItem();
        }

        #endregion

        #region private functions

        private static void OnCommandChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var systemMenuItem = d as SystemMenuItem;

            if (systemMenuItem != null)
            {
                var command = e.NewValue as ICommand;
                if (command != null)
                {
                    systemMenuItem.Command = command;
                }
            }
        }

        #endregion
    }
}
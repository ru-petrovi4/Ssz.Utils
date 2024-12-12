using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;
using Ssz.Operator.Core.Utils;
using Ssz.Operator.Core.ControlsCommon;
using Ssz.Utils;
using Ssz.Utils.Wpf;
using Ssz.Xceed.Wpf.Toolkit.PropertyGrid.Implementation.Editors;
using Microsoft.Extensions.Logging;

namespace Ssz.Operator.Core.VisualEditors.Windows
{
    public partial class PropertiesWindow : LocationMindfulWindow
    {
        #region construction and destruction

        protected PropertiesWindow()
            : base("Properties", 800, 900)
        {
            InitializeComponent();

            CommandBindings.Add(new CommandBinding(InsertNewLineRoutedCommand, InsertNewLineExecuted));
            InsertNewLineRoutedCommand.InputGestures.Add(new KeyGesture(Key.Enter, ModifierKeys.Control));

            Dispatcher.BeginInvoke(DispatcherPriority.Background, new Action(BeginEditing));

            _dispatcherTimer = new DispatcherTimer(new TimeSpan(0, 0, 2), DispatcherPriority.Background,
                (sender, e) => Refresh(),
                Dispatcher);

            _dispatcherTimer.Start();
        }

        #endregion

        #region protected functions

        protected override void OnClosing(CancelEventArgs e)
        {
            base.OnClosing(e);

            _dispatcherTimer.Stop();

            EndEditing();

            Dispatcher.BeginInvoke(DispatcherPriority.Background,
                new Action(() => { ObjectPropertyGrid.SelectedObject = null; }));

            if (_closeAction is not null) _closeAction(this);
            _closeAction = null;

            PropertiesWindows.Remove(this);

            Owner.Activate();
        }

        #endregion

        #region public functions

        public readonly RoutedCommand InsertNewLineRoutedCommand = new();

        public static void Show(Window? ownerWindow, object selectedObject, string? fileFullName,
            Action<Window>? closeAction = null)
        {
            if (selectedObject is null) return;

            var propertiesWindow =
                PropertiesWindows.FirstOrDefault(
                    pw => ReferenceEquals(pw.ObjectPropertyGrid.SelectedObject, selectedObject));
            if (propertiesWindow is not null)
            {
                Dispatcher.CurrentDispatcher.BeginInvoke(new Action(() =>
                {
                    if (propertiesWindow.WindowState == WindowState.Minimized)
                        propertiesWindow.WindowState = WindowState.Normal;
                    propertiesWindow.Activate();
                }));
                return;
            }

            propertiesWindow = null; // PropertiesWindows.FirstOrDefault(pw => pw.PinButton.IsChecked == false);

            if (propertiesWindow is null)
            {
                propertiesWindow = new PropertiesWindow();
                PropertiesWindows.Add(propertiesWindow);
                Dispatcher.CurrentDispatcher.BeginInvoke(new Action(propertiesWindow.Show));
            }
            else
            {
                propertiesWindow.EndEditing();

                propertiesWindow.ObjectPropertyGrid.SelectedObject = null;

                if (propertiesWindow._closeAction is not null) propertiesWindow._closeAction(propertiesWindow);
                propertiesWindow._closeAction = null;

                Dispatcher.CurrentDispatcher.BeginInvoke(new Action(() =>
                {
                    if (propertiesWindow.WindowState == WindowState.Minimized)
                        propertiesWindow.WindowState = WindowState.Normal;
                    propertiesWindow.Activate();
                }));
            }

            propertiesWindow.Owner = ownerWindow;
            propertiesWindow.Title = selectedObject + @" " + Properties.Resources.Properties;
            propertiesWindow._closeAction = closeAction;
            propertiesWindow._fileFullName = fileFullName;

            propertiesWindow.ObjectPropertyGrid.SelectedObject = selectedObject;
            propertiesWindow.ObjectPropertyGrid.SelectedObjectTypeName = selectedObject.ToString();
            propertiesWindow.ObjectPropertyGrid.SelectedObjectName = "";
        }

        public static void ReloadAll()
        {
            foreach (PropertiesWindow propertiesWindow in PropertiesWindows.ToArray()) propertiesWindow.Reload();
        }

        public static void CloseAll()
        {
            foreach (PropertiesWindow propertiesWindow in PropertiesWindows.ToArray()) propertiesWindow.Close();
        }

        public static void CloseAllForFile(string fileFullName)
        {
            if (string.IsNullOrEmpty(fileFullName)) return;
            foreach (PropertiesWindow propertiesWindow in PropertiesWindows.ToArray())
                if (StringHelper.CompareIgnoreCase(propertiesWindow._fileFullName, fileFullName))
                    propertiesWindow.Close();
        }

        #endregion

        #region private functions

        private void BeginEditing()
        {
            var item = ObjectPropertyGrid.SelectedObject as IPropertyGridItem;
            if (item is null || item.RefreshForPropertyGridIsDisabled) return;

            item.RefreshForPropertyGrid();
        }

        private void Refresh()
        {
            var item = ObjectPropertyGrid.SelectedObject as IPropertyGridItem;
            if (item is null || item.RefreshForPropertyGridIsDisabled) return;

            foreach (IPropertyGridItem child in TreeHelper.FindChilds<IPropertyGridItem>(this))
                child.RefreshForPropertyGrid();

            item.RefreshForPropertyGrid();
        }

        private void EndEditing()
        {
            ObjectPropertyGrid.EndEditInPropertyGrid();

            var item = ObjectPropertyGrid.SelectedObject as IPropertyGridItem;
            if (item is null || item.RefreshForPropertyGridIsDisabled) return;

            item.RefreshForPropertyGrid();
        }

        private void Reload()
        {
            var obj = ObjectPropertyGrid.SelectedObject;
            ObjectPropertyGrid.SelectedObject = null;
            ObjectPropertyGrid.SelectedObject = obj;
        }

        private void InsertSpecialCharacterButtonOnClicked(object? sender, RoutedEventArgs e)
        {
            try
            {
                using (var p = new Process())
                {
                    p.StartInfo.FileName = "charmap";
                    p.Start();
                }
            }
            catch (Exception ex)
            {
                DsProject.LoggersSet.Logger.LogError(ex, "Starting charmap failed");
            }
        }

        private void InsertNewLineExecuted(object? sender, ExecutedRoutedEventArgs e)
        {
            var textBox = Keyboard.FocusedElement as TextBox;
            if (textBox is not null && !textBox.IsReadOnly) textBox.InsertNewLine();
        }

        #endregion

        #region private fields

        private static readonly List<PropertiesWindow> PropertiesWindows =
            new();

        private readonly DispatcherTimer _dispatcherTimer;
        private Action<Window>? _closeAction;


        private string? _fileFullName;

        #endregion
    }
}
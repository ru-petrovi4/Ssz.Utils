using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Threading;
using Ssz.Utils;

namespace Ssz.Operator.Core.VisualEditors.Windows
{
    /// <summary>
    ///     The window that shows the properties of one shape, page or drawing.
    ///     <para>
    ///         Ported from the WPF editor. It keeps the same statics the rest of the editor calls -
    ///         Show, ReloadAll, CloseAll, CloseAllForFile - and the same behaviour: asking for the same
    ///         object twice brings the window that already shows it to the front rather than opening a
    ///         second one.
    ///     </para>
    /// </summary>
    public partial class PropertiesWindow : Window
    {
        #region construction and destruction

        public PropertiesWindow()
        {
            InitializeComponent();

            Dispatcher.UIThread.Post(BeginEditing, DispatcherPriority.Background);

            _refreshTimer = new DispatcherTimer(TimeSpan.FromSeconds(2), DispatcherPriority.Background,
                (sender, e) => Refresh());
            _refreshTimer.Start();
        }

        #endregion

        #region public functions

        public static void Show(Window? ownerWindow, object? selectedObject, string? fileFullName,
            Action<Window>? closeAction = null)
        {
            if (selectedObject is null) return;

            PropertiesWindow? propertiesWindow =
                PropertiesWindows.FirstOrDefault(
                    pw => ReferenceEquals(pw.ObjectPropertyGrid.SelectedObject, selectedObject));
            if (propertiesWindow is not null)
            {
                Dispatcher.UIThread.Post(() =>
                {
                    if (propertiesWindow.WindowState == WindowState.Minimized)
                        propertiesWindow.WindowState = WindowState.Normal;
                    propertiesWindow.Activate();
                });
                return;
            }

            propertiesWindow = new PropertiesWindow();
            PropertiesWindows.Add(propertiesWindow);

            propertiesWindow.Title = selectedObject + @" " + Properties.Resources.Properties;
            propertiesWindow._closeAction = closeAction;
            propertiesWindow._fileFullName = fileFullName;

            propertiesWindow.ObjectPropertyGrid.SelectedObject = selectedObject;
            propertiesWindow.ObjectPropertyGrid.SelectedObjectTypeName = selectedObject.ToString();
            propertiesWindow.ObjectPropertyGrid.SelectedObjectName = @"";

            if (ownerWindow is not null)
                propertiesWindow.Show(ownerWindow);
            else
                propertiesWindow.Show();
        }

        public static void ReloadAll()
        {
            foreach (PropertiesWindow propertiesWindow in PropertiesWindows.ToArray()) propertiesWindow.Reload();
        }

        public static void CloseAll()
        {
            foreach (PropertiesWindow propertiesWindow in PropertiesWindows.ToArray()) propertiesWindow.Close();
        }

        public static void CloseAllForFile(string? fileFullName)
        {
            if (String.IsNullOrEmpty(fileFullName)) return;
            foreach (PropertiesWindow propertiesWindow in PropertiesWindows.ToArray())
                if (StringHelper.CompareIgnoreCase(propertiesWindow._fileFullName, fileFullName))
                    propertiesWindow.Close();
        }

        #endregion

        #region protected functions

        protected override void OnClosing(WindowClosingEventArgs e)
        {
            base.OnClosing(e);

            _refreshTimer.Stop();

            EndEditing();

            ObjectPropertyGrid.SelectedObject = null;

            if (_closeAction is not null) _closeAction(this);
            _closeAction = null;

            PropertiesWindows.Remove(this);
        }

        #endregion

        #region private functions

        private void BeginEditing()
        {
            var item = ObjectPropertyGrid.SelectedObject as IPropertyGridItem;
            if (item is null || item.RefreshForPropertyGridIsDisabled) return;

            item.RefreshForPropertyGrid();
        }

        /// <summary>
        ///     A shape may be changed on the drawing while its properties are open - by a drag, or by
        ///     another window - so the values are re-read every couple of seconds, as they were in WPF.
        /// </summary>
        private void Refresh()
        {
            var item = ObjectPropertyGrid.SelectedObject as IPropertyGridItem;
            if (item is null || item.RefreshForPropertyGridIsDisabled) return;

            item.RefreshForPropertyGrid();

            ObjectPropertyGrid.RefreshValues();
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
            object? obj = ObjectPropertyGrid.SelectedObject;
            ObjectPropertyGrid.SelectedObject = null;
            ObjectPropertyGrid.SelectedObject = obj;
        }

        #endregion

        #region private fields

        private static readonly List<PropertiesWindow> PropertiesWindows = new();

        private readonly DispatcherTimer _refreshTimer;
        private Action<Window>? _closeAction;
        private string? _fileFullName;

        #endregion
    }
}

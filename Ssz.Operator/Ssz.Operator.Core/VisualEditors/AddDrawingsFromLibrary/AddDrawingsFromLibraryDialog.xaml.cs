using System;
using System.Collections.Generic;
using System.Windows;
using Ssz.Operator.Core.Drawings;
using Ssz.Operator.Core.ControlsCommon;

namespace Ssz.Operator.Core.VisualEditors.AddDrawingsFromLibrary
{
    public partial class AddDrawingsFromLibraryDialog : LocationMindfulWindow
    {
        #region construction and destruction

        public AddDrawingsFromLibraryDialog()
            : base("AddDrawingsFromStdLibrary", 800, 600)
        {
            InitializeComponent();

            DataContext = new AddDrawingsFromLibraryViewModel();

            MainTreeView.Focus();

            Loaded +=
                (sender, args) =>
                    ((AddDrawingsFromLibraryViewModel) DataContext).GoLibrary(LibraryPathControl.LibraryDirectoryInfo);
        }

        #endregion

        #region public functions

        public IEnumerable<DrawingInfo> DrawingInfos
        {
            get
            {
                var rootItem = (ItemViewModel) MainTreeView.Items[0];
                var drawingInfos = new List<DrawingInfo>();
                rootItem.GetChecked(drawingInfos);
                return drawingInfos;
            }
        }

        #endregion

        #region private functions

        private void OnLibraryDirectoryInfoChanged(object? sender, EventArgs e)
        {
            ((AddDrawingsFromLibraryViewModel) DataContext).GoLibrary(LibraryPathControl.LibraryDirectoryInfo);
        }

        private void UncheckAllButtonOnClick(object? sender, RoutedEventArgs e)
        {
            var rootItem = (ItemViewModel) MainTreeView.Items[0];
            rootItem.IsChecked = false;
            MainTreeView.Focus();
        }

        private void OkButtonOnClick(object? sender, RoutedEventArgs e)
        {
            DialogResult = true;
            Close();
        }

        private void CancelButtonOnClick(object? sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        #endregion
    }
}
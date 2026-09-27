using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Ssz.Operator.Core.ControlsCommon;
using Ssz.Operator.Core.Drawings;

namespace Ssz.Operator.Core.VisualEditors.AddDrawingsFromLibrary
{
    /// <summary>
    ///     Picking pages and shapes out of a library to copy into the project.
    /// </summary>
    public partial class AddDrawingsFromLibraryDialog : LocationMindfulWindow
    {
        #region construction and destruction

        public AddDrawingsFromLibraryDialog()
            : base(@"AddDrawingsFromStdLibrary", 800, 600)
        {
            InitializeComponent();

            DataContext = new AddDrawingsFromLibraryViewModel();

            Loaded += async (sender, args) =>
                await ((AddDrawingsFromLibraryViewModel) DataContext!).GoLibraryAsync(
                    LibraryPathControl.LibraryDirectoryInfo);
        }

        #endregion

        #region public functions

        /// <summary>
        ///     Whether the author accepted what they ticked.
        /// </summary>
        public bool DialogResult { get; private set; }

        public IEnumerable<DrawingInfo> DrawingInfos
        {
            get
            {
                ItemViewModel? rootItem = MainTreeView.ItemsSource?.OfType<ItemViewModel>().FirstOrDefault();
                if (rootItem is null) return Array.Empty<DrawingInfo>();

                var drawingInfos = new List<DrawingInfo>();
                rootItem.GetChecked(drawingInfos);
                return drawingInfos;
            }
        }

        #endregion

        #region private functions

        private async void OnLibraryDirectoryInfoChanged(object? sender, EventArgs e)
        {
            await ((AddDrawingsFromLibraryViewModel) DataContext!).GoLibraryAsync(
                LibraryPathControl.LibraryDirectoryInfo);
        }

        private void UncheckAllButtonOnClick(object? sender, RoutedEventArgs e)
        {
            ItemViewModel? rootItem = MainTreeView.ItemsSource?.OfType<ItemViewModel>().FirstOrDefault();
            if (rootItem is null) return;

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

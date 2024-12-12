using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Threading;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using Ssz.Operator.Core.VisualEditors.AddDrawingsFromLibrary;
using Ssz.Operator.Core.Utils;
using Ssz.Utils;
using Ssz.Utils.Wpf;
using Microsoft.Extensions.Logging;

namespace Ssz.Operator.Core.VisualEditors.SelectImageFromLibrary
{
    public class SelectImageFromLibraryViewModel : ViewModelBase
    {
        #region construction and destruction

        public SelectImageFromLibraryViewModel()
        {
            MainListViewItemsSource = new ObservableCollection<ImageViewModel>();

            BindingOperations.EnableCollectionSynchronization(MainListViewItemsSource,
                _mainListViewItemsSourceSyncRoot);
        }

        #endregion

        #region public functions

        public ObservableCollection<ImageViewModel> MainListViewItemsSource { get; }

        public bool IsBusy
        {
            get => _isBusy;
            set
            {
                if (SetValue(ref _isBusy, value)) OnPropertyChanged(() => IsNotBusy);
            }
        }

        public bool IsNotBusy => !_isBusy;

        public string ProgressString
        {
            get => _progressString;
            set => SetValue(ref _progressString, value);
        }

        public double ProgressPercent
        {
            get => _progressPercent;
            set => SetValue(ref _progressPercent, value);
        }

        public object? SelectedImage { get; set; }

        public void GoLibraryAsync(DirectoryInfo? libraryDirectoryInfo, CancellationToken cancellationToken)
        {
            MainListViewItemsSource.Clear();

            var worker = new BackgroundWorker();
            worker.DoWork += (o, ea) =>
            {
                try
                {
                    if (libraryDirectoryInfo is not null)
                    {
                        List<DirectoryInfo> directories =
                            libraryDirectoryInfo.GetDirectories("*", SearchOption.AllDirectories).ToList();
                        directories.Add(libraryDirectoryInfo);
                        foreach (
                            DirectoryInfo di in directories)
                        {
                            if (cancellationToken.IsCancellationRequested) break;

                            FileInfo[] imageFileInfos =
                                di.GetFilesByExtensions(SearchOption.TopDirectoryOnly, ".png", ".jpeg", ".jpg", ".bmp",
                                    ".gif", ".xaml").ToArray();

                            if (imageFileInfos.Length > 0)
                            {
                                var categoryItem = new ItemViewModel(new EntityInfo(di.Name));

                                var i = 0;
                                foreach (FileInfo imageFileInfo in imageFileInfos)
                                {
                                    if (cancellationToken.IsCancellationRequested) break;

                                    BitmapImage? bitmapImage = null;
                                    string fileFullName = imageFileInfo.FullName;
                                    var fileCreationTimeUtc = imageFileInfo.CreationTimeUtc;
                                    string directoryName = di.Name;
                                    var i2 = i;
                                    Application.Current.Dispatcher.Invoke(
                                        () =>
                                        {
                                            ProgressString = string.Format("{0}: {1}/{2}", directoryName, i2,
                                                imageFileInfos.Length);
                                            ProgressPercent = 100.0 * i2 / imageFileInfos.Length;
                                            bitmapImage = BitmapCache.GetBitmapImage(fileFullName, fileCreationTimeUtc);
                                        }, DispatcherPriority.ContextIdle);

                                    if (bitmapImage is not null)
                                        lock (_mainListViewItemsSourceSyncRoot)
                                        {
                                            MainListViewItemsSource.Add(new ImageViewModel
                                            {
                                                FileInfo = imageFileInfo,
                                                Image = bitmapImage
                                            });
                                        }

                                    i += 1;
                                }
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    DsProject.LoggersSet.Logger.LogError(ex, @"");
                }
            };
            worker.RunWorkerCompleted += (o, ea) => { IsBusy = false; };
            IsBusy = true;
            worker.RunWorkerAsync();
        }

        #endregion

        #region private fields

        private readonly object _mainListViewItemsSourceSyncRoot = new();
        private bool _isBusy;
        private string _progressString = @"";
        private double _progressPercent;

        #endregion
    }

    public class ImageViewModel : ViewModelBase
    {
        #region public functions

        public FileInfo? FileInfo { get; set; }
        public BitmapImage? Image { get; set; }

        #endregion
    }
}
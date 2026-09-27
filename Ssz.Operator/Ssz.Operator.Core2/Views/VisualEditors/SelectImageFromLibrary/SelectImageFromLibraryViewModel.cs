using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Media;
using Avalonia.Threading;
using Microsoft.Extensions.Logging;
using Ssz.Operator.Core.ViewModels;
using Ssz.Utils;

namespace Ssz.Operator.Core.VisualEditors.SelectImageFromLibrary
{
    /// <summary>
    ///     Walks a library folder and shows every picture it holds, so that one can be picked.
    ///     <para>
    ///         Ported from the WPF editor. There the walk ran on a BackgroundWorker and posted each
    ///         thumbnail back to the dispatcher; here it is a task that does its reading away from the
    ///         interface and hands each thumbnail over the same way.
    ///     </para>
    /// </summary>
    public class SelectImageFromLibraryViewModel : ViewModels.ViewModelBase
    {
        #region construction and destruction

        public SelectImageFromLibraryViewModel()
        {
            MainListViewItemsSource = new ObservableCollection<ImageViewModel>();
        }

        #endregion

        #region public functions

        public ObservableCollection<ImageViewModel> MainListViewItemsSource { get; }

        public bool IsBusy
        {
            get => _isBusy;
            set
            {
                if (SetProperty(ref _isBusy, value)) OnPropertyChanged(nameof(IsNotBusy));
            }
        }

        public bool IsNotBusy => !_isBusy;

        public string ProgressString
        {
            get => _progressString;
            set => SetProperty(ref _progressString, value);
        }

        public double ProgressPercent
        {
            get => _progressPercent;
            set => SetProperty(ref _progressPercent, value);
        }

        public object? SelectedImage { get; set; }

        public async void GoLibraryAsync(DirectoryInfo? libraryDirectoryInfo, CancellationToken cancellationToken)
        {
            MainListViewItemsSource.Clear();

            IsBusy = true;

            try
            {
                if (libraryDirectoryInfo is not null)
                {
                    List<DirectoryInfo> directories = await Task.Run(() =>
                    {
                        List<DirectoryInfo> result = libraryDirectoryInfo
                            .GetDirectories(@"*", SearchOption.AllDirectories).ToList();
                        result.Add(libraryDirectoryInfo);
                        return result;
                    }, cancellationToken);

                    foreach (DirectoryInfo di in directories)
                    {
                        if (cancellationToken.IsCancellationRequested) break;

                        FileInfo[] imageFileInfos = await Task.Run(() => di
                            .GetFilesByExtensions(SearchOption.TopDirectoryOnly, @".png", @".jpeg", @".jpg",
                                @".bmp", @".gif", @".xaml")
                            .ToArray(), cancellationToken);

                        if (imageFileInfos.Length == 0) continue;

                        var i = 0;
                        foreach (FileInfo imageFileInfo in imageFileInfos)
                        {
                            if (cancellationToken.IsCancellationRequested) break;

                            ProgressString = $@"{di.Name}: {i}/{imageFileInfos.Length}";
                            ProgressPercent = 100.0 * i / imageFileInfos.Length;

                            IImage? image = BitmapCache.GetBitmapImage(imageFileInfo.FullName,
                                imageFileInfo.CreationTimeUtc);
                            if (image is not null)
                                MainListViewItemsSource.Add(new ImageViewModel
                                {
                                    FileInfo = imageFileInfo,
                                    Image = image
                                });

                            i += 1;

                            // Let the list draw what has arrived, on a library of thousands of files.
                            if (i % 20 == 0) await Dispatcher.UIThread.InvokeAsync(() => { },
                                DispatcherPriority.Background);
                        }
                    }
                }
            }
            catch (OperationCanceledException)
            {
                // The author asked it to stop.
            }
            catch (Exception ex)
            {
                DsProject.LoggersSet.Logger.LogError(ex, @"");
            }
            finally
            {
                IsBusy = false;
            }
        }

        #endregion

        #region private fields

        private bool _isBusy;
        private string _progressString = @"";
        private double _progressPercent;

        #endregion
    }

    /// <summary>
    ///     One picture of the library.
    /// </summary>
    public class ImageViewModel : ViewModels.ViewModelBase
    {
        #region public functions

        public FileInfo? FileInfo { get; set; }

        public IImage? Image { get; set; }

        #endregion
    }
}

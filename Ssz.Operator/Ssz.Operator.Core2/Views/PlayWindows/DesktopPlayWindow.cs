using System;
using System.Collections.Generic;
using System.IO;
using Ssz.Utils; 
using Ssz.Operator.Core;
using Ssz.Operator.Core.Commands;
using Ssz.Operator.Core.Commands.DsCommandOptions;
using Ssz.Operator.Core.ControlsPlay;
using Ssz.Operator.Core.ControlsPlay.VirtualKeyboards;
using Ssz.Operator.Core.DataAccess;
using Ssz.Operator.Core.Addons;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Threading;
using System.ComponentModel;

namespace Ssz.Operator.Core
{
    public class DesktopPlayWindow : Window, IPlayWindow
    {
        #region construction and destruction

        /// <summary>
        ///     rootWindowNum - number of root window starting from 1. If 0, then not root window.
        ///     Not changed during window lifetime.
        /// </summary>
        /// <param name="parentWindow"></param>
        /// <param name="rootWindowNum"></param>
        /// <param name="autoCloseMs"></param>
        public DesktopPlayWindow(IPlayWindow? parentWindow,
            int rootWindowNum,
            int autoCloseMs)
        {
            ParentWindow = parentWindow;
            RootWindowNum = rootWindowNum;

            Content = PlayControlWrapper = new PlayControlWrapper(this); // Because of threading issues

            MainFrame = new Frame(this, @"");

            //var uri = new Uri("pack://application:,,,/Images/Ssz.Operator.ico",
            //    UriKind.RelativeOrAbsolute);
            //Icon = BitmapFrame.Create(uri);
            
            DataContext = new DataValueViewModel(this, false);

            if (autoCloseMs > 0)
            {   
                PointerEntered += (sender, args) =>
                {
                    foreach (var cancellationTokenSource in _autoClose_CancellationTokenSources)
                    {
                        cancellationTokenSource.Cancel();
                    }
                    _autoClose_CancellationTokenSources.Clear();
                };

                PointerExited += (sender, args) =>
                {
                    CancellationTokenSource cts = new();
                    _autoClose_CancellationTokenSources.Add(cts);
                    var cancellationToken = cts.Token;
                    Task.Run(async () =>
                    {
                        await Task.Delay(autoCloseMs);
                        if (!cancellationToken.IsCancellationRequested)
                        {
                            Dispatcher.UIThread.Invoke(() =>
                            {
                                try
                                {
                                    Close();
                                }
                                catch (Exception)
                                {
                                }
                            });
                        }
                    });
                };
            }
        }

        #endregion

        #region public functions        

        public IPlayWindow? ParentWindow { get; private set; }

        /// <summary>
        ///     Depends on RootWindowNum property.
        /// </summary>
        public bool IsRootWindow { get { return RootWindowNum != 0; } }

        /// <summary>
        ///     Number of root window starting from 1. If 0, then not root window.
        ///     Not changed during window lifetime.
        /// </summary>
        public int RootWindowNum { get; private set; }
        
        public PlayControlWrapper PlayControlWrapper { get; }        

        public Frame MainFrame { get; }

        public string WindowCategory { get; set; } = @"";
        
        public CaseInsensitiveOrderedDictionary<List<object?>> WindowVariables { get; } = new();

        #endregion

        #region protected functions

        protected override void OnClosed(EventArgs e)
        {
            base.OnClosed(e);

            PlayControlWrapper.Dispose();

            ((DataValueViewModel) DataContext!).Dispose();
        }

        #endregion

        #region private functions        

        #endregion

        #region private fields

        private readonly List<CancellationTokenSource> _autoClose_CancellationTokenSources = new();

        #endregion
    }
}
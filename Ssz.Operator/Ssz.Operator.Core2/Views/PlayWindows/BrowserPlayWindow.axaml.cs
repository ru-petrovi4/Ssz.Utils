using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Threading;
using Ssz.Operator.Core.ControlsPlay;
using Ssz.Operator.Core.DataAccess;
using Ssz.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Ssz.Operator.Core;

public partial class BrowserPlayWindow : UserControl, IPlayWindow
{
    #region construction and destruction

    /// <summary>
    ///     Vor Visual Designer only
    /// </summary>
    public BrowserPlayWindow()
    {
        InitializeComponent();

        MainFrame = new Frame(this, @"");
    }

    /// <summary>
    ///     rootWindowNum - number of root window starting from 1. If 0, then not root window.
    ///     Not changed during window lifetime.
    /// </summary>
    /// <param name="parentWindow"></param>
    /// <param name="rootWindowNum"></param>
    /// <param name="autoCloseMs"></param>
    public BrowserPlayWindow(IPlayWindow? parentWindow,
        int rootWindowNum,
        int autoCloseMs)
    {
        InitializeComponent();

        ParentWindow = parentWindow;
        RootWindowNum = rootWindowNum;

        MainContentConrol.Content = PlayControlWrapper = new PlayControlWrapper(this); // Because of threading issues

        MainFrame = new Frame(this, @"");

        //var uri = new Uri("pack://application:,,,/Images/Ssz.Operator.ico",
        //    UriKind.RelativeOrAbsolute);
        //Icon = BitmapFrame.Create(uri);

        DataContext = new BrowserPlayWindowViewModel(this, false)
        {
            IsNotRootWindow = !IsRootWindow
        };

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

    public CaseInsensitiveDictionary<List<object?>> WindowVariables { get; } = new();

    public PixelPoint Position
    {
        get
        {
            int x = (int)GetValue(Canvas.LeftProperty);
            if (x < 0)
                x = 0;
            int y = (int)GetValue(Canvas.TopProperty);
            if (y < 0)
                y = 0;
            return new PixelPoint(x, y);
        }
        set
        {
            double x = value.X;
            if (x < 0.0)
                x = 0.0;
            SetValue(Canvas.LeftProperty, x);
            double y = value.Y;
            if (y < 0.0)
                y = 0.0;
            SetValue(Canvas.TopProperty, y);
        }
    }

    public WindowState WindowState { get; set; }

    public bool IsActive { get; set; }

    public event EventHandler? Activated;

    public event EventHandler<WindowClosingEventArgs>? Closing;

    public event EventHandler? Closed;

    public void Activate()
    {
        IsActive = true;
    }

    public void Close()
    { 
        Closed?.Invoke(this, EventArgs.Empty);

        PlayControlWrapper.Dispose();

        ((DataValueViewModel)DataContext!).Dispose();
    }

    #endregion

    #region protected functions    

    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        if (!IsRootWindow && Parent != null)
        {
            _isPressed = true;
            _pressedWindowPosition = Position;
            _pressedPointerPosition = e.GetPosition((Visual)Parent);
        }

        base.OnPointerPressed(e);
    }

    protected override void OnPointerReleased(PointerReleasedEventArgs e)
    {
        if (!IsRootWindow)
        {
            _isPressed = false;
        }

        base.OnPointerReleased(e);
    }

    protected override void OnPointerMoved(PointerEventArgs e)
    {
        if (!IsRootWindow)
        {
            if (_isPressed && Parent != null)
            {
                var currentPointerPosition = e.GetPosition((Visual)Parent);

                Position = new PixelPoint(
                    _pressedWindowPosition.X + (int)(currentPointerPosition.X - _pressedPointerPosition.X),
                    _pressedWindowPosition.Y + (int)(currentPointerPosition.Y - _pressedPointerPosition.Y)
                    );
            }
        }

        base.OnPointerMoved(e);
    }

    #endregion

    #region private functions  

    private void CloseButton_OnClick(object? sender, RoutedEventArgs args)
    {
        Close();
    }

    #endregion

    #region private fields

    private readonly List<CancellationTokenSource> _autoClose_CancellationTokenSources = new();

    private bool _isPressed;
    private Point _pressedPointerPosition;
    private PixelPoint _pressedWindowPosition;

    #endregion
}
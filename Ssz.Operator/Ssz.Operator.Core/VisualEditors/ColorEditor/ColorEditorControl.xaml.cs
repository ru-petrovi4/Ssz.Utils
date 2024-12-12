using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using Microsoft.Win32;
using Ssz.Operator.Core.Constants;
using Ssz.Operator.Core.Utils;
using Ssz.Utils.Wpf;
using Control = System.Windows.Forms.Control;
using KeyEventArgs = System.Windows.Input.KeyEventArgs;
using UserControl = System.Windows.Controls.UserControl;

namespace Ssz.Operator.Core.VisualEditors.ColorEditor
{
    public partial class ColorEditorControl : UserControl
    {
        #region construction and destruction

        public ColorEditorControl()
        {
            InitializeComponent();

            _colorEditorViewModel = new ColorEditorViewModel();

            MainStackPanel.DataContext = _colorEditorViewModel;

            Unloaded += (sender, args) => StopEyeDropperMode();

            _virtualScreenLeftInPixels = (int) SystemParameters.VirtualScreenLeft;
            _virtualScreenTopInPixels = (int) SystemParameters.VirtualScreenTop;
            _virtualScreenWidthInPixels = (int) SystemParameters.VirtualScreenWidth;
            _virtualScreenHeightInPixels = (int) SystemParameters.VirtualScreenHeight;

            _dispatcherTimer = new DispatcherTimer();
            _dispatcherTimer.Interval = TimeSpan.FromMilliseconds(50);
            _dispatcherTimer.Tick += DispatcherTimerOnTick;

            _colorEditorViewModel.SelectedColorChanged += OnSelectedColorChanged;
        }

        #endregion

        #region protected functions

        protected override void OnPreviewKeyDown(KeyEventArgs e)
        {
            if (e.Key == Key.Escape) StopEyeDropperMode();
            base.OnPreviewKeyDown(e);
        }

        #endregion

        #region public functions

        public static readonly DependencyProperty SelectedColorProperty = DependencyProperty.Register(
            @"SelectedColor",
            typeof(Color),
            typeof(ColorEditorControl),
            new FrameworkPropertyMetadata(default(Color), FrameworkPropertyMetadataOptions.BindsTwoWayByDefault,
                OnSelectedColorPropertyChanged));

        public Color SelectedColor
        {
            get => (Color) GetValue(SelectedColorProperty);
            set => SetValue(SelectedColorProperty, value);
        }

        public string? SelectedColorString
        {
            get
            {
                string constant = ConstantTextBox.Text;
                if (ConstantsHelper.ContainsQuery(constant)) return constant;
                var selectedColor = _colorEditorViewModel.SelectedColor;
                return ObsoleteAnyHelper.ConvertTo<string>(selectedColor, false);
            }
            set
            {
                if (ConstantsHelper.ContainsQuery(value))
                {
                    _colorEditorViewModel.SelectedColor = default;
                    ConstantTextBox.Text = value;
                }
                else
                {
                    if (string.IsNullOrWhiteSpace(value))
                    {
                        _colorEditorViewModel.SelectedColor = Colors.White;
                    }
                    else
                    {
                        var color = ObsoleteAnyHelper.ConvertTo<Color>(value, false);
                        if (color == default)
                            _colorEditorViewModel.SelectedColor = Colors.White;
                        else
                            _colorEditorViewModel.SelectedColor = color;
                    }

                    ConstantTextBox.Text = "";
                }
            }
        }

        public void HideConstants()
        {
            ConstantTextBlock.Visibility = Visibility.Hidden;
            ConstantTextBox.Visibility = Visibility.Hidden;
        }

        #endregion

        #region private functions

        private static void OnSelectedColorPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ((ColorEditorControl) d)._colorEditorViewModel.SelectedColor = (Color) e.NewValue;
        }

        private void EyedropperButtonOnClick(object? sender, RoutedEventArgs e)
        {
            _tickCount = 0;
            _dispatcherTimer.Start();

            #region Change Cursor

            var pRegKey = Registry.CurrentUser;
            pRegKey = pRegKey?.OpenSubKey(@"Control Panel\Cursors");
            _cursorPaths.Clear();
            if (pRegKey is not null)
                foreach (string valueName in pRegKey.GetValueNames())
                    try
                    {
                        var cursorPath = pRegKey.GetValue(valueName) as string;
                        if (cursorPath is null) continue;
                        //Take a backup.
                        _cursorPaths.Add(valueName, cursorPath);
                        Registry.SetValue(@"HKEY_CURRENT_USER\Control Panel\Cursors", valueName,
                            AppDomain.CurrentDomain.BaseDirectory + @"Resources\Images\eyedropper.cur");
                    }
                    catch (Exception)
                    {
                    }

            InteropHelper.SystemParametersInfo(InteropHelper.SPI_SETCURSORS, 0, null,
                InteropHelper.SPIF_UPDATEINIFILE | InteropHelper.SPIF_SENDCHANGE);

            #endregion
        }

        private void StopEyeDropperMode()
        {
            _dispatcherTimer.Stop();

            foreach (string valueName in _cursorPaths.Keys)
            {
                string cursorPath = _cursorPaths[valueName];
                Registry.SetValue(@"HKEY_CURRENT_USER\Control Panel\Cursors", valueName, cursorPath);
            }

            _cursorPaths.Clear();

            InteropHelper.SystemParametersInfo(InteropHelper.SPI_SETCURSORS, 0, null,
                InteropHelper.SPIF_UPDATEINIFILE | InteropHelper.SPIF_SENDCHANGE);
        }

        private void TakeScreenPicture()
        {
            _screenBitmapSource = InteropHelper.CaptureRegion(_virtualScreenLeftInPixels, _virtualScreenTopInPixels,
                _virtualScreenWidthInPixels, _virtualScreenHeightInPixels);

            if (_screenBitmapSource is not null && _screenBitmapSource.Format != PixelFormats.Bgra32)
                _screenBitmapSource = new FormatConvertedBitmap(_screenBitmapSource, PixelFormats.Bgra32, null, 0);

            if (_screenBitmapSource is not null)
            {
                _widthInPixels = _screenBitmapSource.PixelWidth;
                _heightInPixels = _screenBitmapSource.PixelHeight;
                _stride = _widthInPixels * 4;
                _pixelBytes = new byte[_heightInPixels * _stride];
            }
        }

        private void DispatcherTimerOnTick(object? sender, EventArgs e)
        {
            if (_tickCount % 60 == 0) TakeScreenPicture();
            _tickCount += 1;

            var mousePositionInPixels = Control.MousePosition;
            var pointInPixels = new Point(mousePositionInPixels.X, mousePositionInPixels.Y);
            if (_previousPointInPixels is null || _previousPointInPixels != pointInPixels)
                if (_screenBitmapSource is not null)
                {
                    var x = mousePositionInPixels.X - _virtualScreenLeftInPixels;
                    var y = mousePositionInPixels.Y - _virtualScreenTopInPixels;
                    var rect = new Int32Rect(x, y, 1, 1);
                    _screenBitmapSource.CopyPixels(rect, _pixelBytes, _stride, 0);
                    _colorEditorViewModel.SelectedColor = Color.FromArgb(_pixelBytes[3], _pixelBytes[2],
                        _pixelBytes[1], _pixelBytes[0]);
                }

            if (Control.MouseButtons == MouseButtons.Left) StopEyeDropperMode();
            _previousPointInPixels = pointInPixels;
        }

        private void ConstantTextBoxOnKeyUp(object? sender, KeyEventArgs e)
        {
            if (ConstantTextBox.Text.Length > 0) _colorEditorViewModel.SelectedColor = default;
        }

        private void OnSelectedColorChanged()
        {
            if (_colorEditorViewModel.SelectedColor != default) ConstantTextBox.Text = "";

            SelectedColor = _colorEditorViewModel.SelectedColor;
        }

        #endregion

        #region private fields

        private readonly ColorEditorViewModel _colorEditorViewModel;
        private BitmapSource? _screenBitmapSource;
        private readonly DispatcherTimer _dispatcherTimer;
        private int _tickCount;
        private Point? _previousPointInPixels;
        private readonly Dictionary<string, string> _cursorPaths = new();
        private int _widthInPixels;
        private int _heightInPixels;
        private int _stride;
        private byte[] _pixelBytes = new byte[0];
        private readonly int _virtualScreenLeftInPixels;
        private readonly int _virtualScreenTopInPixels;
        private readonly int _virtualScreenWidthInPixels;
        private readonly int _virtualScreenHeightInPixels;

        #endregion
    }
}
using System.ComponentModel;
using System.Drawing;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;

namespace Ssz.Utils.Wpf.WpfMessageBox
{
    internal class MessageBoxViewModel : INotifyPropertyChanged
    {
        #region construction and destruction

        public MessageBoxViewModel(
            WpfMessageBoxWindow view,
            string title,
            string message,
            WpfMessageBoxButton buttonOption,
            MessageBoxImage image,
            WpfMessageBoxResult defaultResult,
            MessageBoxOptions options)
        {
            //TextAlignment
            _title = title;
            Message = message;
            ButtonOption = buttonOption;
            Options = options;

            SetDirections(options);
            SetButtonVisibility(buttonOption);
            SetImageSource(image);
            SetButtonDefault(defaultResult);
            _view = view;
        }

        #endregion

        #region public functions

        public event PropertyChangedEventHandler? PropertyChanged;
        public WpfMessageBoxResult Result { get; set; }

        public WpfMessageBoxButton ButtonOption
        {
            get { return _buttonOption; }
            set
            {
                if (_buttonOption != value)
                {
                    _buttonOption = value;
                    NotifyPropertyChange("ButtonOption");
                }
            }
        }

        public MessageBoxOptions Options
        {
            get { return _options; }
            set
            {
                if (_options != value)
                {
                    _options = value;
                    NotifyPropertyChange("Options");
                }
            }
        }

        public string Title
        {
            get { return _title; }
            set
            {
                if (_title != value)
                {
                    _title = value;
                    NotifyPropertyChange("Title");
                }
            }
        }

        public string Message
        {
            get { return _message; }
            set
            {
                if (_message != value)
                {
                    _message = value;
                    NotifyPropertyChange("Message");
                }
            }
        }

        public ImageSource? MessageImageSource
        {
            get { return _messageImageSource; }
            set
            {
                _messageImageSource = value;
                NotifyPropertyChange("MessageImageSource");
            }
        }

        public Visibility YesNoVisibility
        {
            get { return _yesNoVisibility; }
            set
            {
                if (_yesNoVisibility != value)
                {
                    _yesNoVisibility = value;
                    NotifyPropertyChange("YesNoVisibility");
                }
            }
        }

        public Visibility YesForAllNoForAllVisibility
        {
            get { return _yesForAllNoForAllVisibility; }
            set
            {
                if (_yesForAllNoForAllVisibility != value)
                {
                    _yesForAllNoForAllVisibility = value;
                    NotifyPropertyChange("YesForAllNoForAllVisibility");
                }
            }
        }

        public Visibility CancelVisibility
        {
            get { return _cancelVisibility; }
            set
            {
                if (_cancelVisibility != value)
                {
                    _cancelVisibility = value;
                    NotifyPropertyChange("CancelVisibility");
                }
            }
        }

        public Visibility OkVisibility
        {
            get { return _okVisibility; }
            set
            {
                if (_okVisibility != value)
                {
                    _okVisibility = value;
                    NotifyPropertyChange("OkVisibility");
                }
            }
        }

        public HorizontalAlignment ContentTextAlignment
        {
            get { return _contentTextAlignment; }
            set
            {
                if (_contentTextAlignment != value)
                {
                    _contentTextAlignment = value;
                    NotifyPropertyChange("ContentTextAlignment");
                }
            }
        }

        public FlowDirection ContentFlowDirection
        {
            get { return _contentFlowDirection; }
            set
            {
                if (_contentFlowDirection != value)
                {
                    _contentFlowDirection = value;
                    NotifyPropertyChange("ContentFlowDirection");
                }
            }
        }


        public FlowDirection TitleFlowDirection
        {
            get { return _titleFlowDirection; }
            set
            {
                if (_titleFlowDirection != value)
                {
                    _titleFlowDirection = value;
                    NotifyPropertyChange("TitleFlowDirection");
                }
            }
        }


        public bool IsOkDefault
        {
            get { return _isOkDefault; }
            set
            {
                if (_isOkDefault != value)
                {
                    _isOkDefault = value;
                    NotifyPropertyChange("IsOkDefault");
                }
            }
        }

        public bool IsYesDefault
        {
            get { return _isYesDefault; }
            set
            {
                if (_isYesDefault != value)
                {
                    _isYesDefault = value;
                    NotifyPropertyChange("IsYesDefault");
                }
            }
        }

        public bool IsNoDefault
        {
            get { return _isNoDefault; }
            set
            {
                if (_isNoDefault != value)
                {
                    _isNoDefault = value;
                    NotifyPropertyChange("IsNoDefault");
                }
            }
        }

        public bool IsYesForAllDefault
        {
            get { return _isYesForAllDefault; }
            set
            {
                if (_isYesForAllDefault != value)
                {
                    _isYesForAllDefault = value;
                    NotifyPropertyChange("IsYesForAllDefault");
                }
            }
        }

        public bool IsNoForAllDefault
        {
            get { return _isNoForAllDefault; }
            set
            {
                if (_isNoForAllDefault != value)
                {
                    _isNoForAllDefault = value;
                    NotifyPropertyChange("IsNoForAllDefault");
                }
            }
        }

        public bool IsCancelDefault
        {
            get { return _isCancelDefault; }
            set
            {
                if (_isCancelDefault != value)
                {
                    _isCancelDefault = value;
                    NotifyPropertyChange("IsCancelDefault");
                }
            }
        }

        // called when the yes button is pressed
        public ICommand YesCommand
        {
            get
            {
                if (_yesCommand is null)
                    _yesCommand = new DelegateCommand(() =>
                    {
                        Result = WpfMessageBoxResult.Yes;
                        _view.Close();
                    });
                return _yesCommand;
            }
        }

        // called when the no button is pressed
        public ICommand NoCommand
        {
            get
            {
                if (_noCommand is null)
                    _noCommand = new DelegateCommand(() =>
                    {
                        Result = WpfMessageBoxResult.No;
                        _view.Close();
                    });
                return _noCommand;
            }
        }

        // called when the yes button is pressed
        public ICommand YesForAllCommand
        {
            get
            {
                if (_yesForAllCommand is null)
                    _yesForAllCommand = new DelegateCommand(() =>
                    {
                        Result = WpfMessageBoxResult.YesForAll;
                        _view.Close();
                    });
                return _yesForAllCommand;
            }
        }

        // called when the no button is pressed
        public ICommand NoForAllCommand
        {
            get
            {
                if (_noForAllCommand is null)
                    _noForAllCommand = new DelegateCommand(() =>
                    {
                        Result = WpfMessageBoxResult.NoForAll;
                        _view.Close();
                    });
                return _noForAllCommand;
            }
        }

        // called when the cancel button is pressed
        public ICommand CancelCommand
        {
            get
            {
                if (_cancelCommand is null)
                    _cancelCommand = new DelegateCommand(() =>
                    {
                        Result = WpfMessageBoxResult.Cancel;
                        _view.Close();
                    });
                return _cancelCommand;
            }
        }

        // called when the ok button is pressed
        public ICommand OkCommand
        {
            get
            {
                if (_okCommand is null)
                    _okCommand = new DelegateCommand(() =>
                    {
                        Result = WpfMessageBoxResult.OK;
                        _view.Close();
                    });
                return _okCommand;
            }
        }

        // called when the escape key is pressed
        public ICommand EscapeCommand
        {
            get
            {
                if (_escapeCommand is null)
                    _escapeCommand = new DelegateCommand(() =>
                    {
                        switch (ButtonOption)
                        {
                            case WpfMessageBoxButton.OK:
                                Result = WpfMessageBoxResult.OK;
                                _view.Close();
                                break;

                            case WpfMessageBoxButton.YesNoCancel:
                            case WpfMessageBoxButton.OKCancel:
                            case WpfMessageBoxButton.YesNoYesAllNoAllCancel:
                                Result = WpfMessageBoxResult.Cancel;
                                _view.Close();
                                break;

                            case WpfMessageBoxButton.YesNo:
                                // ignore close
                                break;

                            default:
                                break;
                        }
                    });
                return _escapeCommand;
            }
        }

        // called when the form is closed by via close button click or programmatically
        public ICommand CloseCommand
        {
            get
            {
                if (_closeCommand is null)
                    _closeCommand = new DelegateCommand(() =>
                    {
                        if (Result == WpfMessageBoxResult.None)
                        {
                            switch (ButtonOption)
                            {
                                case WpfMessageBoxButton.OK:
                                    Result = WpfMessageBoxResult.OK;
                                    break;

                                case WpfMessageBoxButton.YesNoCancel:
                                case WpfMessageBoxButton.OKCancel:
                                case WpfMessageBoxButton.YesNoYesAllNoAllCancel:
                                    Result = WpfMessageBoxResult.Cancel;
                                    break;

                                case WpfMessageBoxButton.YesNo:
                                    // ignore close
                                    break;

                                default:
                                    break;
                            }
                        }
                    });
                return _closeCommand;
            }
        }

        #endregion

        #region private functions

        private void SetDirections(MessageBoxOptions options)
        {
            switch (options)
            {
                case MessageBoxOptions.None:
                    ContentTextAlignment = HorizontalAlignment.Left;
                    ContentFlowDirection = FlowDirection.LeftToRight;
                    TitleFlowDirection = FlowDirection.LeftToRight;
                    break;

                case MessageBoxOptions.RightAlign:
                    ContentTextAlignment = HorizontalAlignment.Right;
                    ContentFlowDirection = FlowDirection.LeftToRight;
                    TitleFlowDirection = FlowDirection.LeftToRight;
                    break;

                case MessageBoxOptions.RtlReading:
                    ContentTextAlignment = HorizontalAlignment.Right;
                    ContentFlowDirection = FlowDirection.RightToLeft;
                    TitleFlowDirection = FlowDirection.RightToLeft;
                    break;

                case MessageBoxOptions.RightAlign | MessageBoxOptions.RtlReading:
                    ContentTextAlignment = HorizontalAlignment.Left;
                    ContentFlowDirection = FlowDirection.RightToLeft;
                    TitleFlowDirection = FlowDirection.RightToLeft;
                    break;
            }
        }

        private void NotifyPropertyChange(string property)
        {
            if (PropertyChanged is not null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(property));
            }
        }

        private void SetButtonDefault(WpfMessageBoxResult defaultResult)
        {
            switch (defaultResult)
            {
                case WpfMessageBoxResult.OK:
                    IsOkDefault = true;
                    break;

                case WpfMessageBoxResult.Yes:
                    IsYesDefault = true;
                    break;

                case WpfMessageBoxResult.No:
                    IsNoDefault = true;
                    break;

                case WpfMessageBoxResult.YesForAll:
                    IsYesForAllDefault = true;
                    break;

                case WpfMessageBoxResult.NoForAll:
                    IsNoForAllDefault = true;
                    break;

                case WpfMessageBoxResult.Cancel:
                    IsCancelDefault = true;
                    break;

                default:
                    break;
            }
        }

        private void SetButtonVisibility(WpfMessageBoxButton buttonOption)
        {
            switch (buttonOption)
            {
                case WpfMessageBoxButton.YesNo:
                    YesForAllNoForAllVisibility = OkVisibility = CancelVisibility = Visibility.Collapsed;
                    break;

                case WpfMessageBoxButton.YesNoCancel:
                    YesForAllNoForAllVisibility = OkVisibility = Visibility.Collapsed;
                    break;

                case WpfMessageBoxButton.YesNoYesAllNoAllCancel:
                    OkVisibility = Visibility.Collapsed;
                    break;

                case WpfMessageBoxButton.OK:
                    YesNoVisibility = YesForAllNoForAllVisibility = CancelVisibility = Visibility.Collapsed;
                    break;

                case WpfMessageBoxButton.OKCancel:
                    YesNoVisibility = YesForAllNoForAllVisibility = Visibility.Collapsed;
                    break;

                default:
                    OkVisibility =
                        CancelVisibility = YesNoVisibility = YesForAllNoForAllVisibility = Visibility.Collapsed;
                    break;
            }
        }

        private void SetImageSource(MessageBoxImage image)
        {
            switch (image)
            {
                    //case MessageBoxImage.Hand:
                    //case MessageBoxImage.Stop:
                case MessageBoxImage.Error:
                    MessageImageSource = SystemIcons.Error.ToImageSource();
                    break;

                    //case MessageBoxImage.Exclamation:
                case MessageBoxImage.Warning:
                    MessageImageSource = SystemIcons.Warning.ToImageSource();
                    break;

                case MessageBoxImage.Question:
                    MessageImageSource = SystemIcons.Question.ToImageSource();
                    break;

                    //case MessageBoxImage.Asterisk:
                case MessageBoxImage.Information:
                    MessageImageSource = SystemIcons.Information.ToImageSource();
                    break;

                default:
                    MessageImageSource = null;
                    break;
            }
        }

        #endregion

        #region private fields

        private bool _isOkDefault;
        private bool _isYesDefault;
        private bool _isNoDefault;
        private bool _isYesForAllDefault;
        private bool _isNoForAllDefault;
        private bool _isCancelDefault;

        private string _title;
        private string _message = "";
        private WpfMessageBoxButton _buttonOption;
        private MessageBoxOptions _options;

        private Visibility _yesNoVisibility;
        private Visibility _yesForAllNoForAllVisibility;
        private Visibility _cancelVisibility;
        private Visibility _okVisibility;

        private HorizontalAlignment _contentTextAlignment;
        private FlowDirection _contentFlowDirection;
        private FlowDirection _titleFlowDirection;

        private ICommand? _yesCommand;
        private ICommand? _noCommand;
        private ICommand? _yesForAllCommand;
        private ICommand? _noForAllCommand;
        private ICommand? _cancelCommand;
        private ICommand? _okCommand;
        private ICommand? _escapeCommand;
        private ICommand? _closeCommand;

        private readonly WpfMessageBoxWindow _view;
        private ImageSource? _messageImageSource;

        #endregion
    }
}
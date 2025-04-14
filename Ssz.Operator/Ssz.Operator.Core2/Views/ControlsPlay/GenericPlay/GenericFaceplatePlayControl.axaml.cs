using Ssz.Operator.Core.Commands.DsCommandOptions;
using Ssz.Operator.Core.DsPageTypes;

namespace Ssz.Operator.Core.ControlsPlay.GenericPlay
{
    public partial class GenericFaceplatePlayControl : FaceplatePlayControlBase
    {
        #region construction and destruction

        public GenericFaceplatePlayControl(IPlayWindow playWindow)
            : base(playWindow)
        {
            InitializeComponent();
        }

        #endregion

        #region protected functions

        protected override PlayDsPageDrawingViewbox PlayDsPageDrawingViewbox
        {
            get => (PlayDsPageDrawingViewbox) Content!;
            set => Content = value;
        }

        #endregion

        #region public functions

        public override bool PrepareWindow(IPlayWindow newWindow,
            ref ShowWindowDsCommandOptions showWindowDsCommandOptions)
        {
            if (DsPageDrawing is null) return true;

            var genericFaceplateDsPageType = DsPageDrawing.DsPageTypeObject as GenericFaceplateDsPageType;
            if (genericFaceplateDsPageType is null)
                genericFaceplateDsPageType = new GenericFaceplateDsPageType();

            if (string.IsNullOrEmpty(showWindowDsCommandOptions.WindowCategory))
                showWindowDsCommandOptions.WindowCategory = genericFaceplateDsPageType.WindowCategory;
            if (string.IsNullOrEmpty(showWindowDsCommandOptions.FrameName))
                showWindowDsCommandOptions.FrameName = genericFaceplateDsPageType.FrameName;
            if (showWindowDsCommandOptions.ShowOnTouchScreen == DefaultFalseTrue.Default)
            {
                if (genericFaceplateDsPageType.ShowOnTouchScreen == DefaultFalseTrue.Default)
                    showWindowDsCommandOptions.ShowOnTouchScreen = DefaultFalseTrue.False;
                else
                    showWindowDsCommandOptions.ShowOnTouchScreen = genericFaceplateDsPageType.ShowOnTouchScreen;
            }

            if (showWindowDsCommandOptions.WindowStyle == PlayWindowStyle.Default)
            {
                if (genericFaceplateDsPageType.WindowStyle == PlayWindowStyle.Default)
                    showWindowDsCommandOptions.WindowStyle = PlayWindowStyle.ToolWindow;
                else
                    showWindowDsCommandOptions.WindowStyle = genericFaceplateDsPageType.WindowStyle;
            }

            if (showWindowDsCommandOptions.WindowResizeMode == PlayWindowResizeMode.Default)
                if (genericFaceplateDsPageType.WindowResizeMode != PlayWindowResizeMode.Default)
                    showWindowDsCommandOptions.WindowResizeMode = genericFaceplateDsPageType.WindowResizeMode;
            if (showWindowDsCommandOptions.WindowStartupLocation == PlayWindowStartupLocation.Default)
            {
                if (genericFaceplateDsPageType.WindowStartupLocation == PlayWindowStartupLocation.Default)
                    showWindowDsCommandOptions.WindowStartupLocation = PlayWindowStartupLocation.UpperLeft;
                else
                    showWindowDsCommandOptions.WindowStartupLocation =
                        genericFaceplateDsPageType.WindowStartupLocation;
            }

            if (showWindowDsCommandOptions.WindowFullScreen == DefaultFalseTrue.Default)
            {
                if (genericFaceplateDsPageType.WindowFullScreen == DefaultFalseTrue.Default)
                    showWindowDsCommandOptions.WindowFullScreen = DefaultFalseTrue.False;
                else
                    showWindowDsCommandOptions.WindowFullScreen = genericFaceplateDsPageType.WindowFullScreen;
            }

            if (!showWindowDsCommandOptions.ContentWidth.HasValue)
                showWindowDsCommandOptions.ContentWidth = DsPageDrawing.Width;
            if (!showWindowDsCommandOptions.ContentHeight.HasValue)
                showWindowDsCommandOptions.ContentHeight = DsPageDrawing.Height;
            if (showWindowDsCommandOptions.TitleInfo.IsConst &&
                string.IsNullOrEmpty(showWindowDsCommandOptions.TitleInfo.ConstValue))
                showWindowDsCommandOptions.TitleInfo.ConstValue =
                    genericFaceplateDsPageType.TitleInfo.ConstValue;
            if (showWindowDsCommandOptions.WindowShowInTaskbar == DefaultFalseTrue.Default)
            {
                if (genericFaceplateDsPageType.WindowShowInTaskbar == DefaultFalseTrue.Default)
                    showWindowDsCommandOptions.WindowShowInTaskbar = DefaultFalseTrue.True;
                else
                    showWindowDsCommandOptions.WindowShowInTaskbar = genericFaceplateDsPageType.WindowShowInTaskbar;
            }

            if (showWindowDsCommandOptions.WindowTopmost == DefaultFalseTrue.Default)
            {
                if (genericFaceplateDsPageType.WindowTopmost == DefaultFalseTrue.Default)
                    showWindowDsCommandOptions.WindowTopmost = DefaultFalseTrue.True;
                else
                    showWindowDsCommandOptions.WindowTopmost = genericFaceplateDsPageType.WindowTopmost;
            }

            if (showWindowDsCommandOptions.AutoCloseMs == 0)
            {
                showWindowDsCommandOptions.AutoCloseMs = genericFaceplateDsPageType.AutoCloseMs;
            }

            return false;
        }

        #endregion
    }
}
#pragma warning disable

namespace Fluent.Localization.Languages
{
    [RibbonLocalization("Persian", "fa")]
    public class Persian : RibbonLocalizationBase
    {
        public override string Automatic { get; } = "خودکار";
        public override string BackstageButtonKeyTip { get; } = "ف";
        public override string BackstageButtonText { get; } = "فایل";
        public override string CustomizeStatusBar { get; } = FallbackLocalization.CustomizeStatusBar /* Customize Status Bar */;
        public override string DisplayOptionsButtonScreenTipText { get; } = FallbackLocalization.DisplayOptionsButtonScreenTipText /* Configure Ribbon display options. */;
        public override string DisplayOptionsButtonScreenTipTitle { get; } = FallbackLocalization.DisplayOptionsButtonScreenTipTitle /* Ribbon Display Options */;
        public override string ExpandRibbon { get; } = FallbackLocalization.ExpandRibbon /* Expand the Ribbon */;
        public override string MinimizeRibbon { get; } = FallbackLocalization.MinimizeRibbon /* Minimize the Ribbon */;
        public override string MoreColors { get; } = "رنگهای بیشتر...";
        public override string NoColor { get; } = "بدون رنگ";
        public override string QuickAccessToolBarDropDownButtonTooltip { get; } = "دلخواه سازی میله ابزار دسترسی سریع";
        public override string QuickAccessToolBarMenuHeader { get; } = "دلخواه سازی میله ابزار دسترسی سریع";
        public override string QuickAccessToolBarMenuShowAbove { get; } = "نمایش در بالای نوار";
        public override string QuickAccessToolBarMenuShowBelow { get; } = "نمایش در پایین نوار";
        public override string QuickAccessToolBarMoreControlsButtonTooltip { get; } = "ابزارهای دیگر";
        public override string RibbonContextMenuAddGallery { get; } = "اضافه کردن گالری به میله ابزار دسترسی سریع";
        public override string RibbonContextMenuAddGroup { get; } = "اضافه کردن گروه به میله ابزار دسترسی سریع";
        public override string RibbonContextMenuAddItem { get; } = "اضافه کردن به میله ابزار دسترسی سریع";
        public override string RibbonContextMenuAddMenu { get; } = "اضاقه کردن منو به میله ابزار دسترسی سریع";
        public override string RibbonContextMenuCustomizeQuickAccessToolBar { get; } = "دلخواه سازی میله ابزار دسترسی سریع...";
        public override string RibbonContextMenuCustomizeRibbon { get; } = "دلخواه سازی نوار...";
        public override string RibbonContextMenuMinimizeRibbon { get; } = "کوچک کردن نوار";
        public override string RibbonContextMenuRemoveItem { get; } = "حذف از میله ابزار دسترسی سریع";
        public override string RibbonContextMenuShowAbove { get; } = "نمایش میله ابزار دسترسی سریع در بالای نوار";
        public override string RibbonContextMenuShowBelow { get; } = "نمایش میله ابزار دسترسی سریع در پایین نوار";
        public override string RibbonLayout { get; } = FallbackLocalization.RibbonLayout /* Ribbon Layout */;
        public override string ScreenTipDisableReasonHeader { get; } = FallbackLocalization.ScreenTipDisableReasonHeader /* This command is currently disabled. */;
        public override string ScreenTipF1LabelHeader { get; } = FallbackLocalization.ScreenTipF1LabelHeader /* Press F1 for help */;
        public override string ShowRibbon { get; } = FallbackLocalization.ShowRibbon /* Show Ribbon */;
        public override string UseClassicRibbon { get; } = "_از نوار کلاسیک استفاده کنید";
        public override string UseSimplifiedRibbon { get; } = "_از نوارهای ساده استفاده کنید";
    }
}
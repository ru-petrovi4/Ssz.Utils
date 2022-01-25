#pragma warning disable

namespace Fluent.Localization.Languages
{
    [RibbonLocalization("Chinese", "zh")]
    public class Chinese : RibbonLocalizationBase
    {
        public override string Automatic { get; } = "自动";
        public override string BackstageButtonKeyTip { get; } = FallbackLocalization.BackstageButtonKeyTip /* F */;
        public override string BackstageButtonText { get; } = "文件";
        public override string CustomizeStatusBar { get; } = "自定义状态栏";
        public override string DisplayOptionsButtonScreenTipText { get; } = FallbackLocalization.DisplayOptionsButtonScreenTipText /* Configure Ribbon display options. */;
        public override string DisplayOptionsButtonScreenTipTitle { get; } = FallbackLocalization.DisplayOptionsButtonScreenTipTitle /* Ribbon Display Options */;
        public override string ExpandRibbon { get; } = FallbackLocalization.ExpandRibbon /* Expand the Ribbon */;
        public override string MinimizeRibbon { get; } = FallbackLocalization.MinimizeRibbon /* Minimize the Ribbon */;
        public override string MoreColors { get; } = "更多的颜色...";
        public override string NoColor { get; } = "没有颜色";
        public override string QuickAccessToolBarDropDownButtonTooltip { get; } = "自定义快速访问具栏";
        public override string QuickAccessToolBarMenuHeader { get; } = "自定义快速访问工具栏";
        public override string QuickAccessToolBarMenuShowAbove { get; } = "在功能区上方显示";
        public override string QuickAccessToolBarMenuShowBelow { get; } = "在功能区下方显示";
        public override string QuickAccessToolBarMoreControlsButtonTooltip { get; } = "其他命令";
        public override string RibbonContextMenuAddGallery { get; } = "在快速访问工具栏中添加样式";
        public override string RibbonContextMenuAddGroup { get; } = "在快速访问工具栏中添加组";
        public override string RibbonContextMenuAddItem { get; } = "添加到快速访问工具栏";
        public override string RibbonContextMenuAddMenu { get; } = "在快速访问工具栏中添加菜单";
        public override string RibbonContextMenuCustomizeQuickAccessToolBar { get; } = "自定义快速访问工具栏...";
        public override string RibbonContextMenuCustomizeRibbon { get; } = "自定义功能区...";
        public override string RibbonContextMenuMinimizeRibbon { get; } = "功能区最小化";
        public override string RibbonContextMenuRemoveItem { get; } = "在快速访问工具栏中移除";
        public override string RibbonContextMenuShowAbove { get; } = "在功能区上方显示快速访问工具栏";
        public override string RibbonContextMenuShowBelow { get; } = "在功能区下方显示快速访问工具栏";
        public override string RibbonLayout { get; } = FallbackLocalization.RibbonLayout /* Ribbon Layout */;
        public override string ScreenTipDisableReasonHeader { get; } = "此命令当前已被禁用。";
        public override string ScreenTipF1LabelHeader { get; } = FallbackLocalization.ScreenTipF1LabelHeader /* Press F1 for help */;
        public override string ShowRibbon { get; } = FallbackLocalization.ShowRibbon /* Show Ribbon */;
        public override string UseClassicRibbon { get; } = "使用经典功能区(_U)";
        public override string UseSimplifiedRibbon { get; } = "使用简化功能区(_U)";
    }
}
#pragma warning disable

namespace Fluent.Localization.Languages
{
    [RibbonLocalization("Czech", "cs")]
    public class Czech : RibbonLocalizationBase
    {
        public override string Automatic { get; } = "Automatické";
        public override string BackstageButtonKeyTip { get; } = "S";
        public override string BackstageButtonText { get; } = "Soubor";
        public override string CustomizeStatusBar { get; } = "Přizpůsobit Status Bar";
        public override string DisplayOptionsButtonScreenTipText { get; } = FallbackLocalization.DisplayOptionsButtonScreenTipText /* Configure Ribbon display options. */;
        public override string DisplayOptionsButtonScreenTipTitle { get; } = FallbackLocalization.DisplayOptionsButtonScreenTipTitle /* Ribbon Display Options */;
        public override string ExpandRibbon { get; } = FallbackLocalization.ExpandRibbon /* Expand the Ribbon */;
        public override string MinimizeRibbon { get; } = FallbackLocalization.MinimizeRibbon /* Minimize the Ribbon */;
        public override string MoreColors { get; } = "Další barvy...";
        public override string NoColor { get; } = "Bez barvy";
        public override string QuickAccessToolBarDropDownButtonTooltip { get; } = "Přizpůsobit panel nástrojů Rychlý přístup";
        public override string QuickAccessToolBarMenuHeader { get; } = "Přizpůsobit panel nástrojů Rychlý přístup";
        public override string QuickAccessToolBarMenuShowAbove { get; } = "Zobrazit nad pásem karet";
        public override string QuickAccessToolBarMenuShowBelow { get; } = "Zobrazit pod pásem karet";
        public override string QuickAccessToolBarMoreControlsButtonTooltip { get; } = "Další příkazy";
        public override string RibbonContextMenuAddGallery { get; } = "Přidat galerii na panel nástrojů Rychlý přístup";
        public override string RibbonContextMenuAddGroup { get; } = "Přidat na panel nástrojů Rychlý přístup";
        public override string RibbonContextMenuAddItem { get; } = "Přidat na panel nástrojů Rychlý přístup";
        public override string RibbonContextMenuAddMenu { get; } = "Přidat na panel nástrojů Rychlý přístup";
        public override string RibbonContextMenuCustomizeQuickAccessToolBar { get; } = "Přizpůsobit panel nástrojů Rychlý přístup...";
        public override string RibbonContextMenuCustomizeRibbon { get; } = "Přizpůsobit pás karet...";
        public override string RibbonContextMenuMinimizeRibbon { get; } = "Skrýt pás karet";
        public override string RibbonContextMenuRemoveItem { get; } = "Odebrat z panelu nástrojů Rychlý přístup";
        public override string RibbonContextMenuShowAbove { get; } = "Zobrazit panel nástrojů Rychlý přístup nad pásem karet";
        public override string RibbonContextMenuShowBelow { get; } = "Zobrazit panel nástrojů Rychlý přístup pod pásem karet";
        public override string RibbonLayout { get; } = FallbackLocalization.RibbonLayout /* Ribbon Layout */;
        public override string ScreenTipDisableReasonHeader { get; } = "Tento příkaz je aktuálně zakázán.";
        public override string ScreenTipF1LabelHeader { get; } = "Stiskni F1 pro nápovědu";
        public override string ShowRibbon { get; } = FallbackLocalization.ShowRibbon /* Show Ribbon */;
        public override string UseClassicRibbon { get; } = "Po_užívat klasický pás karet";
        public override string UseSimplifiedRibbon { get; } = "Po_užívat zjednodušený pás karet";
    }
}
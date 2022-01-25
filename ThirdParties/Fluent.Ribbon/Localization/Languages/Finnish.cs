#pragma warning disable

namespace Fluent.Localization.Languages
{
    [RibbonLocalization("Finnish", "fi")]
    public class Finnish : RibbonLocalizationBase
    {
        public override string Automatic { get; } = "Automaattinen";
        public override string BackstageButtonKeyTip { get; } = "T";
        public override string BackstageButtonText { get; } = "Tiedosto";
        public override string CustomizeStatusBar { get; } = FallbackLocalization.CustomizeStatusBar /* Customize Status Bar */;
        public override string DisplayOptionsButtonScreenTipText { get; } = FallbackLocalization.DisplayOptionsButtonScreenTipText /* Configure Ribbon display options. */;
        public override string DisplayOptionsButtonScreenTipTitle { get; } = FallbackLocalization.DisplayOptionsButtonScreenTipTitle /* Ribbon Display Options */;
        public override string ExpandRibbon { get; } = FallbackLocalization.ExpandRibbon /* Expand the Ribbon */;
        public override string MinimizeRibbon { get; } = FallbackLocalization.MinimizeRibbon /* Minimize the Ribbon */;
        public override string MoreColors { get; } = "Lisää värejä...";
        public override string NoColor { get; } = "Ei väri";
        public override string QuickAccessToolBarDropDownButtonTooltip { get; } = "Mukauta pikatyökaluriviä";
        public override string QuickAccessToolBarMenuHeader { get; } = "Mukauta pikatyökaluriviä";
        public override string QuickAccessToolBarMenuShowAbove { get; } = "Näytä valintanauhan yläpuolella";
        public override string QuickAccessToolBarMenuShowBelow { get; } = "Näytä valintanauhan alapuolella";
        public override string QuickAccessToolBarMoreControlsButtonTooltip { get; } = "Lisää valintoja";
        public override string RibbonContextMenuAddGallery { get; } = "Lisää valikoima pikatyökaluriviin";
        public override string RibbonContextMenuAddGroup { get; } = "Lisää ryhmä pikatyökaluriviin";
        public override string RibbonContextMenuAddItem { get; } = "Lisää pikatyökaluriville";
        public override string RibbonContextMenuAddMenu { get; } = "Lisää valikko pikatyökaluriviin";
        public override string RibbonContextMenuCustomizeQuickAccessToolBar { get; } = "Mukauta pikatyökaluriviä...";
        public override string RibbonContextMenuCustomizeRibbon { get; } = "Mukauta valintanauhaa...";
        public override string RibbonContextMenuMinimizeRibbon { get; } = "Pienennä valintanauha";
        public override string RibbonContextMenuRemoveItem { get; } = "Poista pikatyökaluriviltä";
        public override string RibbonContextMenuShowAbove { get; } = "Näytä pikatyökalurivi valintanauhan yläpuolella";
        public override string RibbonContextMenuShowBelow { get; } = "Näytä pikatyökalurivi valintanauhan alapuolella";
        public override string RibbonLayout { get; } = FallbackLocalization.RibbonLayout /* Ribbon Layout */;
        public override string ScreenTipDisableReasonHeader { get; } = "Tämä komento on tällä hetkellä poissa käytöstä";
        public override string ScreenTipF1LabelHeader { get; } = FallbackLocalization.ScreenTipF1LabelHeader /* Press F1 for help */;
        public override string ShowRibbon { get; } = FallbackLocalization.ShowRibbon /* Show Ribbon */;
        public override string UseClassicRibbon { get; } = "_Käytä perinteistä valintanauhaa";
        public override string UseSimplifiedRibbon { get; } = "_Käytä yksinkertaistettua valintanauhaa";
    }
}
#pragma warning disable

namespace Fluent.Localization.Languages
{
    [RibbonLocalization("Portuguese", "pt")]
    public class Portuguese : RibbonLocalizationBase
    {
        public override string Automatic { get; } = "Automático";
        public override string BackstageButtonKeyTip { get; } = FallbackLocalization.BackstageButtonKeyTip /* F */;
        public override string BackstageButtonText { get; } = "Ficheiro";
        public override string CustomizeStatusBar { get; } = FallbackLocalization.CustomizeStatusBar /* Customize Status Bar */;
        public override string DisplayOptionsButtonScreenTipText { get; } = FallbackLocalization.DisplayOptionsButtonScreenTipText /* Configure Ribbon display options. */;
        public override string DisplayOptionsButtonScreenTipTitle { get; } = FallbackLocalization.DisplayOptionsButtonScreenTipTitle /* Ribbon Display Options */;
        public override string ExpandRibbon { get; } = FallbackLocalization.ExpandRibbon /* Expand the Ribbon */;
        public override string MinimizeRibbon { get; } = FallbackLocalization.MinimizeRibbon /* Minimize the Ribbon */;
        public override string MoreColors { get; } = "Mais cores...";
        public override string NoColor { get; } = "Nenhuma cor";
        public override string QuickAccessToolBarDropDownButtonTooltip { get; } = "Personalizar Barra de Ferramentas de Acesso Rápido";
        public override string QuickAccessToolBarMenuHeader { get; } = "Personalizar Barra de Ferramentas de Acesso Rápido";
        public override string QuickAccessToolBarMenuShowAbove { get; } = "Mostrar Acima do Friso";
        public override string QuickAccessToolBarMenuShowBelow { get; } = "Mostrar Abaixo do Friso";
        public override string QuickAccessToolBarMoreControlsButtonTooltip { get; } = "Mais Comandos...";
        public override string RibbonContextMenuAddGallery { get; } = "Adicionar Galeria à Barra de Ferramentas de Acesso Rápido";
        public override string RibbonContextMenuAddGroup { get; } = "Adicionar Grupo à Barra de Ferramentas de Acesso Rápido";
        public override string RibbonContextMenuAddItem { get; } = "Adicionar à Barra de Ferramentas de Acesso Rápido";
        public override string RibbonContextMenuAddMenu { get; } = "Adicionar Menu à Barra de Ferramentas de Acesso Rápido";
        public override string RibbonContextMenuCustomizeQuickAccessToolBar { get; } = "Personalizar Barra de Ferramentas de Acesso Rápido...";
        public override string RibbonContextMenuCustomizeRibbon { get; } = "Personalizar o Friso...";
        public override string RibbonContextMenuMinimizeRibbon { get; } = "Minimizar o Friso";
        public override string RibbonContextMenuRemoveItem { get; } = "Remover da Barra de Ferramentas de Acesso Rápido";
        public override string RibbonContextMenuShowAbove { get; } = "Mostrar Barra de Ferramentas de Acesso Rápido Acima do Friso";
        public override string RibbonContextMenuShowBelow { get; } = "Mostrar Barra de Ferramentas de Acesso Rápido Abaixo do Friso";
        public override string RibbonLayout { get; } = FallbackLocalization.RibbonLayout /* Ribbon Layout */;
        public override string ScreenTipDisableReasonHeader { get; } = "Este comando está desactivado actualmente.";
        public override string ScreenTipF1LabelHeader { get; } = FallbackLocalization.ScreenTipF1LabelHeader /* Press F1 for help */;
        public override string ShowRibbon { get; } = FallbackLocalization.ShowRibbon /* Show Ribbon */;
        public override string UseClassicRibbon { get; } = "_Utilizar o Friso Clássico";
        public override string UseSimplifiedRibbon { get; } = "_Utilizar o Friso Simplificado";
    }
}
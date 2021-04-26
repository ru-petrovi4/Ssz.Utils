namespace Fluent.Automation.Peers
{
    

    /// <summary>
    ///     Automation peer for <see cref="InRibbonGallery" />
    /// </summary>
    // todo: add full automation for expansion, listing items (?) etc.
    public class RibbonInRibbonGalleryAutomationPeer : RibbonHeaderedControlAutomationPeer
    {
        /// <summary>
        ///     Creates a new instance.
        /// </summary>
        public RibbonInRibbonGalleryAutomationPeer(InRibbonGallery owner)
            : base(owner)
        {
        }

        /// <inheritdoc />
        protected override string GetClassNameCore()
        {
            return this.Owner.GetType().Name;
        }
    }
}
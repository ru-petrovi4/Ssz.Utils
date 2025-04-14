using System.Threading.Tasks;

namespace Ssz.Operator.Core
{
    public abstract class ToolkitOperation
    {
        #region public functions

        public abstract string RibbonGroup { get; }

        public abstract string ButtonText { get; }

        public abstract string ButtonToolTip { get; }

        public virtual bool CloseAllDrawings => true;

        public abstract Task<ToolkitOperationResult> DoWork(IProgressInfo progressInfo, object? parameter, bool silent);

        #endregion
    }
}
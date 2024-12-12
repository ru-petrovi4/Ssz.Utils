using System.Collections.Generic;
using Ssz.Operator.Core.Utils;

namespace Ssz.Operator.Core.Commands
{
    public interface ICommandFactory
    {
        IEnumerable<string> GetCommands();

        OwnedDataSerializableAndCloneable? NewDsCommandOptionsObject(string command);
    }
}
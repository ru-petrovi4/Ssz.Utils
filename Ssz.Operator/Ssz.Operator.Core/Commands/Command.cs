using Ssz.Operator.Core.ControlsPlay;

namespace Ssz.Operator.Core.Commands
{
    public class Command
    {
        #region construction and destruction

        public Command(Frame? senderFrame, string commandString, object? commandOptions = null)
        {
            SenderFrame = senderFrame;
            CommandString = commandString;
            CommandOptions = commandOptions;
        }

        #endregion

        #region public functions

        public Frame? SenderFrame { get; }

        public string CommandString { get; }

        public object? CommandOptions { get; }

        public bool StopFurtherHandling { get; set; }

        #endregion
    }
}
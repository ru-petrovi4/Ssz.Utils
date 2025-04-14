using System;
using Avalonia.Controls;
using Ssz.Operator.Core.Addons;
using Ssz.Operator.Core.Commands;
using Ssz.Operator.Core.Commands.DsCommandOptions;
using Ssz.Operator.Core.ControlsCommon;
using Ssz.Operator.Core.ControlsPlay;
using Ssz.Operator.Core.DsShapes;

namespace Ssz.Operator.Core.DsShapeViews
{
    public class AlarmListDsShapeView : ControlDsShapeView<Control>
    {
        #region private functions

        private void CommandsManagerOnGotCommand(Command command)
        {
            if (command.CommandString == CommandsManager.AckCommand && command.CommandOptions is not null &&
                    string.IsNullOrWhiteSpace(((GenericDsCommandOptions) command.CommandOptions).ParamsString))
                ((IAlarmListControl) Control).AckAlarms();
        }

        #endregion

        #region construction and destruction

        public AlarmListDsShapeView(AlarmListDsShape dsShape, ControlsPlay.Frame? frame)
            : base(
                new Func<Control>(() =>
                {
                    switch (dsShape.Type)
                    {
                        case AlarmListDsShape.AppearanceType.Generic:
                            return new GenericAlarmListControl(DsProject.Instance.GetAddon<GenericEmulationAddon>()
                                .AlarmsListViewModel.Alarms);
                        default:
                            throw new InvalidOperationException();
                    }
                })(),
                dsShape, frame
            )
        {
            CommandsManager.AddCommandHandler(CommandsManagerOnGotCommand);
        }


        protected override void Dispose(bool disposing)
        {
            if (Disposed) return;
            if (disposing) CommandsManager.RemoveCommandHandler(CommandsManagerOnGotCommand);
            // Release unmanaged resources.
            // Set large fields to null.    

            base.Dispose(disposing);
        }

        #endregion
    }
}
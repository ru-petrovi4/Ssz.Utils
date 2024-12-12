using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Logging;
using Ssz.Operator.Core.Addons;
using Ssz.Operator.Core.Commands.DsCommandOptions;
using Ssz.Operator.Core.ControlsPlay;
using Ssz.Operator.Core.Properties;
using Ssz.Operator.Core.Utils;

namespace Ssz.Operator.Core.Commands
{
    public static class CommandsManager
    {
        #region public functions

        public const string JumpCommand = "Jump";
        public const string JumpBackCommand = "JumpBack";
        public const string JumpForwardCommand = "JumpForward";
        public const string ShowWindowCommand = "ShowWindow";
        public const string ShowFaceplateForTagCommand = "ShowFaceplateForTag";
        public const string CloseWindowCommand = "CloseWindow";
        public const string CloseAllFaceplatesCommand = "CloseAllFaceplates";
        public const string ApplyCommand = "Apply";
        public const string SetValueCommand = "SetValue";
        public const string ShowNewRootWindowCommand = "ShowNewRootWindow";
        public const string SetupTouchScreenCommand = "SetupTouchScreen";
        public const string ShowVirtualKeyboardCommand = "ShowVirtualKeyboard";
        public const string AckCommand = "Ack";
        public const string AckTagCommand = "AckTag";
        public const string AckAllCommand = "AckAll";
        public const string BuzzerResetCommand = "BuzzerReset";
        public const string BuzzerEnableCommand = "BuzzerEnable";
        public const string CommandsListCommand = "CommandsList";
        public const string StartProcessCommand = "StartProcess";
        public const string SendKeyCommand = "SendKey";
        public const string FindCommand = "Find";
        public const string PanoramaJumpCommand = "PanoramaJump";
        public const string PanoramaShowMapCommand = "PanoramaShowMap";
        public const string PanoramaFindPathCommand = "PanoramaFindPath";
        public const string RampUpUpCommand = @"RampUpUp";
        public const string RampUpCommand = @"RampUp";        
        public const string RampDownCommand = @"RampDown";
        public const string RampDownDownCommand = @"RampDownDown";
        public const string SetModeToManualCommand = @"SetModeToManual";
        public const string SetModeToAutoCommand = @"SetModeToAuto";
        public const string SetModeToCascadeCommand = @"SetModeToCascade";
        //public const string HighlightShapeCommand = "HighlightShape";
        //public const string ResetHighlightedShapesCommand = "ResetHighlightedShapes";

        public static void AddCommandHandler(Action<Command> commandHandler, int group = 0)
        {
            lock (CommandHandlersSyncRoot)
            {
                List<Action<Command>>? commandHandlersList;
                CommandHandlers.TryGetValue(group, out commandHandlersList);
                if (commandHandlersList is null)
                {
                    commandHandlersList = new List<Action<Command>>();
                    CommandHandlers.Add(group, commandHandlersList);
                }

                commandHandlersList.Add(commandHandler);
            }
        }

        public static void RemoveCommandHandler(Action<Command> commandHandler)
        {
            if (commandHandler is null) return;

            lock (CommandHandlersSyncRoot)
            {
                foreach (var commandHandlersList in CommandHandlers.Values)
                {
                    var removed = commandHandlersList.Remove(commandHandler);
                    if (removed) return;
                }
            }
        }

        public static void NotifyCommand(Frame? senderFrame, string commandString, object? dsCommandOptions = null)
        {
            var command = new Command(senderFrame, commandString, dsCommandOptions);

            var commandHandlers = new List<Action<Command>>();
            lock (CommandHandlersSyncRoot)
            {
                foreach (var kvp in CommandHandlers.OrderBy(i => i.Key)) commandHandlers.AddRange(kvp.Value);
            }

            foreach (var commandHandler in commandHandlers)
            {
                try
                {
                    commandHandler.Invoke(command);
                }
                catch (Exception ex)
                {
                    DsProject.LoggersSet.Logger.LogError(ex, commandString + @" Command handling error");
                    MessageBoxHelper.ShowError(commandString + @" Command error. " + Resources.SeeErrorLogForDetails);
                }

                if (command.StopFurtherHandling) return;
            }
        }


        public static IEnumerable<string> GetAvailableCommands()
        {
            var commands = new List<string>
            {
                JumpCommand,
                JumpBackCommand,
                JumpForwardCommand,
                ShowWindowCommand,
                ShowFaceplateForTagCommand,
                CloseWindowCommand,
                CloseAllFaceplatesCommand,
                ApplyCommand,
                SetValueCommand,
                AckCommand,
                AckTagCommand,
                BuzzerResetCommand,
                BuzzerEnableCommand,
                CommandsListCommand,
                StartProcessCommand,
                SendKeyCommand,
                FindCommand,
                PanoramaJumpCommand,
                PanoramaShowMapCommand,
                PanoramaFindPathCommand,
                RampUpUpCommand,
                RampUpCommand,
                RampDownCommand,
                RampDownDownCommand,
                SetModeToManualCommand,
                SetModeToAutoCommand,
                SetModeToCascadeCommand,
            };
            commands.AddRange(AddonsHelper.GetCommands());
            return commands;
        }


        public static OwnedDataSerializableAndCloneable? NewDsCommandOptionsObject(string commandString)
        {
            if (string.IsNullOrEmpty(commandString)) return null;

            switch (commandString)
            {
                case JumpCommand:
                    return new JumpDsCommandOptions();
                case JumpBackCommand:
                    return new JumpBackDsCommandOptions();
                case JumpForwardCommand:
                    return new JumpForwardDsCommandOptions();
                case ShowWindowCommand:
                    return new ShowWindowDsCommandOptions();
                case ShowFaceplateForTagCommand:
                    return new ShowFaceplateForTagDsCommandOptions();
                case CloseWindowCommand:
                    return new CloseWindowDsCommandOptions();
                case CloseAllFaceplatesCommand:
                    return new CloseAllFaceplatesDsCommandOptions();
                case ApplyCommand:
                    return new ApplyDsCommandOptions();
                case SetValueCommand:
                    return new SetValueDsCommandOptions();
                case AckCommand:
                    return new GenericDsCommandOptions();
                case AckTagCommand:
                    return new GenericDsCommandOptions();
                case BuzzerResetCommand:
                    return new GenericDsCommandOptions();
                case BuzzerEnableCommand:
                    return new OnOffToggleDsCommandOptions();
                case CommandsListCommand:
                    return new CommandsListDsCommandOptions();
                case StartProcessCommand:
                    return new StartProcessDsCommandOptions();
                case SendKeyCommand:
                    return new SendKeyDsCommandOptions();
                case FindCommand:
                    return new GenericDsCommandOptions();
                case PanoramaJumpCommand:
                    return new PanoramaJumpDsCommandOptions();
                case PanoramaShowMapCommand:
                    return new GenericDsCommandOptions();
                case PanoramaFindPathCommand:
                    return new GenericDsCommandOptions();
                case RampUpUpCommand:
                    return new GenericDsCommandOptions();
                case RampUpCommand:
                    return new GenericDsCommandOptions();
                case RampDownCommand:
                    return new GenericDsCommandOptions();
                case RampDownDownCommand:
                    return new GenericDsCommandOptions();
                case SetModeToManualCommand:
                    return new GenericDsCommandOptions();
                case SetModeToAutoCommand:
                    return new GenericDsCommandOptions();
                case SetModeToCascadeCommand:
                    return new GenericDsCommandOptions();
                default:
                    return AddonsHelper.NewDsCommandOptionsObject(commandString);
            }
        }

        #endregion

        #region private functions

        private static readonly Dictionary<int, List<Action<Command>>> CommandHandlers = new();
        private static readonly object CommandHandlersSyncRoot = new();

        #endregion
    }
}
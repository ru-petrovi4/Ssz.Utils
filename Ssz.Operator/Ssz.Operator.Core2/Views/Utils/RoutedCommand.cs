using System;
using System.Collections.Generic;
using System.Windows.Input;
using Avalonia.Input;

namespace Ssz.Operator.Core.Utils
{
    /// <summary>
    ///     A command of the editor, and the arguments its handlers take.
    ///     <para>
    ///         WPF routed a command to whichever element under the focus had a binding for it, and asked
    ///         that binding both whether it can run and to run it. Every binding in this editor pointed
    ///         at the same two objects, so the routing carried no information: what it amounted to is a
    ///         command with an Executed and a CanExecute handler. That is what this is, which lets the
    ///         handlers - there are about a hundred of them - keep the signature they were written with.
    ///     </para>
    ///     <para>
    ///         The other thing WPF did by itself was re-ask every command whether it can still run,
    ///         whenever anything happened. Avalonia has no such sweep, so the editor calls
    ///         <see cref="InvalidateRequerySuggested" /> at the moments that change the answer: the
    ///         selection, the focused drawing, the undo stack.
    ///     </para>
    /// </summary>
    public class RoutedCommand : ICommand
    {
        #region construction and destruction

        public RoutedCommand()
        {
            lock (AllCommands)
                AllCommands.Add(new WeakReference<RoutedCommand>(this));
        }

        #endregion

        #region public functions

        /// <summary>
        ///     The keys that run this command. The editor turns them into Avalonia key bindings on its
        ///     root view.
        /// </summary>
        public List<KeyGesture> InputGestures { get; } = new();

        public Action<object?, ExecutedRoutedEventArgs>? Executed { get; set; }

        public Action<object?, CanExecuteRoutedEventArgs>? CanExecuteHandler { get; set; }

        public bool CanExecute(object? parameter)
        {
            if (Executed is null) return false;
            if (CanExecuteHandler is null) return true;

            var args = new CanExecuteRoutedEventArgs(parameter);
            CanExecuteHandler(this, args);
            return args.CanExecute;
        }

        public void Execute(object? parameter)
        {
            if (Executed is null || !CanExecute(parameter)) return;

            Executed(this, new ExecutedRoutedEventArgs(parameter));
        }

        public event EventHandler? CanExecuteChanged;

        public void NotifyCanExecuteChanged()
        {
            CanExecuteChanged?.Invoke(this, EventArgs.Empty);
        }

        /// <summary>
        ///     Asks every command of the editor to tell its buttons again whether it can run.
        /// </summary>
        public static void InvalidateRequerySuggested()
        {
            lock (AllCommands)
            {
                for (var i = AllCommands.Count - 1; i >= 0; i--)
                    if (AllCommands[i].TryGetTarget(out RoutedCommand? command))
                        command.NotifyCanExecuteChanged();
                    else
                        AllCommands.RemoveAt(i);
            }
        }

        #endregion

        #region private fields

        private static readonly List<WeakReference<RoutedCommand>> AllCommands = new();

        #endregion
    }

    public class ExecutedRoutedEventArgs : EventArgs
    {
        public ExecutedRoutedEventArgs(object? parameter)
        {
            Parameter = parameter;
        }

        public object? Parameter { get; }

        public bool Handled { get; set; }
    }

    public class CanExecuteRoutedEventArgs : EventArgs
    {
        public CanExecuteRoutedEventArgs(object? parameter)
        {
            Parameter = parameter;
        }

        public object? Parameter { get; }

        public bool CanExecute { get; set; }

        public bool Handled { get; set; }
    }

    /// <summary>
    ///     Joins a command to the pair of handlers that answer it.
    /// </summary>
    public class CommandBinding
    {
        #region construction and destruction

        public CommandBinding(RoutedCommand command,
            Action<object?, ExecutedRoutedEventArgs> executed,
            Action<object?, CanExecuteRoutedEventArgs>? canExecute = null)
        {
            Command = command;
            Executed = executed;
            CanExecute = canExecute;
        }

        #endregion

        #region public functions

        public RoutedCommand Command { get; }

        public Action<object?, ExecutedRoutedEventArgs> Executed { get; }

        public Action<object?, CanExecuteRoutedEventArgs>? CanExecute { get; }

        /// <summary>
        ///     Makes the command answer through this binding. In WPF this happened while the element
        ///     holding the binding had the focus; here the editor has one set of bindings, so it holds
        ///     for as long as they are added.
        /// </summary>
        public void Attach()
        {
            Command.Executed = Executed;
            Command.CanExecuteHandler = CanExecute;
            Command.NotifyCanExecuteChanged();
        }

        public void Detach()
        {
            if (ReferenceEquals(Command.Executed, Executed))
            {
                Command.Executed = null;
                Command.CanExecuteHandler = null;
                Command.NotifyCanExecuteChanged();
            }
        }

        #endregion
    }

    public class CommandBindingCollection : List<CommandBinding>
    {
        #region public functions

        public new void Add(CommandBinding commandBinding)
        {
            base.Add(commandBinding);
            commandBinding.Attach();
        }

        public new bool Remove(CommandBinding commandBinding)
        {
            commandBinding.Detach();
            return base.Remove(commandBinding);
        }

        #endregion
    }
}

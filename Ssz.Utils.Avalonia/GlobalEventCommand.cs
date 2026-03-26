using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Input;

namespace Ssz.Utils.Avalonia;

public class GlobalEventCommand : ICommand
{
    // Событие, на которое будут подписываться другие части приложения
    public event Action<object?>? Executed;

    // ICommand требует наличия этого события, но для безусловных команд оно не используется
    public event EventHandler? CanExecuteChanged;

    public bool CanExecute(object? parameter)
    {
        return true; // Команда доступна всегда
    }

    public void Execute(object? parameter)
    {
        // При вызове команды вызываем наше C#-событие и передаем параметр (например, ViewModel)
        Executed?.Invoke(parameter);
    }
}

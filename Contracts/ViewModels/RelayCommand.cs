using System.Windows.Input;

namespace Contracts.ViewModels;

public sealed class RelayCommand(Action<object?> exec, Func<object?, bool>? can = null) : ICommand
{
    private readonly Action<object?> _exec = exec;
    private readonly Func<object?, bool>? _can = can;

    public bool CanExecute(object? parameter) => _can?.Invoke(parameter) ?? true;
    public void Execute(object? parameter) => _exec(parameter);

    public event EventHandler? CanExecuteChanged;

    public void RaiseCanExecuteChanged() =>
        CanExecuteChanged?.Invoke(this, EventArgs.Empty);
}
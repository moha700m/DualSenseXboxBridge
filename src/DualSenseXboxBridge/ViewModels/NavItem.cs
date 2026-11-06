using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;

namespace DualSenseXboxBridge.ViewModels;

public sealed class NavItem : INotifyPropertyChanged
{
    private bool _isActive;

    public NavItem(string title, ICommand command)
    {
        Title = title;
        Command = command;
    }

    public string Title { get; }

    public ICommand Command { get; }

    public bool IsActive
    {
        get => _isActive;
        set
        {
            if (_isActive == value)
            {
                return;
            }

            _isActive = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsActive)));
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;
}

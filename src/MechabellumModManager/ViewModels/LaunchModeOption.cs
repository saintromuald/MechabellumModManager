using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace MechabellumModManager.ViewModels;

public sealed class LaunchModeOption : INotifyPropertyChanged
{
    string _label;

    public LaunchModeOption(Models.LaunchMode mode, string label)
    {
        Mode = mode;
        _label = label;
    }

    public Models.LaunchMode Mode { get; }

    public string Label
    {
        get => _label;
        set
        {
            if (_label == value) return;
            _label = value;
            OnPropertyChanged();
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    void OnPropertyChanged([CallerMemberName] string? name = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

    public override string ToString() => Label;
}

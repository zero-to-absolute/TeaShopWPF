using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace TeaShopManager.Models;

public sealed class NavigationItem : INotifyPropertyChanged
{
    private bool _hasAttentionMark;

    public string SectionKey { get; init; } = string.Empty;

    public string Title { get; init; } = string.Empty;

    public string IconGlyph { get; init; } = string.Empty;

    public bool HasAttentionMark
    {
        get => _hasAttentionMark;
        set
        {
            if (_hasAttentionMark == value)
            {
                return;
            }

            _hasAttentionMark = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(HasAttentionMark)));
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;
}

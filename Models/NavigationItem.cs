namespace TeaShopManager.Models;

public sealed class NavigationItem
{
    public string SectionKey { get; init; } = string.Empty;

    public string Title { get; init; } = string.Empty;

    public string IconGlyph { get; init; } = string.Empty;

    public bool HasAttentionMark { get; init; }
}

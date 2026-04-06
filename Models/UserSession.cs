namespace TeaShopManager.Models;

public sealed class UserSession
{
    public int UserId { get; init; }

    public string Login { get; init; } = string.Empty;

    public string FullName { get; init; } = string.Empty;

    public string RoleName { get; init; } = string.Empty;

    public string DisplayName => string.IsNullOrWhiteSpace(FullName) ? Login : FullName;

    public string Initials
    {
        get
        {
            var source = DisplayName.Trim();
            if (string.IsNullOrWhiteSpace(source))
            {
                return "TS";
            }

            var parts = source
                .Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Take(2)
                .Select(part => char.ToUpperInvariant(part[0]));

            var initials = string.Concat(parts);
            return string.IsNullOrWhiteSpace(initials)
                ? source[..Math.Min(2, source.Length)].ToUpperInvariant()
                : initials;
        }
    }
}

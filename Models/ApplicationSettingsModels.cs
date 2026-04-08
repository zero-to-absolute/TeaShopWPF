namespace TeaShopManager.Models;

public sealed class ApplicationSettingsModel
{
    public int ExpirationWarningDays { get; set; } = 14;
}

public sealed class DatabaseConnectionInfo
{
    public string ServerName { get; init; } = string.Empty;

    public string DatabaseName { get; init; } = string.Empty;

    public string AuthenticationMode { get; init; } = string.Empty;
}

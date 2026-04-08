using System.IO;
using System.Text.Json;
using TeaShopManager.Models;

namespace TeaShopManager.DataAccess;

public sealed class ApplicationSettingsStore
{
    private const int DefaultExpirationWarningDays = 14;
    private readonly string _settingsPath;

    public ApplicationSettingsStore(string? settingsPath = null)
    {
        _settingsPath = settingsPath ?? Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "TeaShopManager",
            "user-settings.json");
    }

    public ApplicationSettingsModel Load()
    {
        if (!File.Exists(_settingsPath))
        {
            return CreateDefault();
        }

        try
        {
            var json = File.ReadAllText(_settingsPath);
            var settings = JsonSerializer.Deserialize<ApplicationSettingsModel>(json);
            return Normalize(settings);
        }
        catch
        {
            return CreateDefault();
        }
    }

    public void Save(ApplicationSettingsModel settings)
    {
        var normalized = Normalize(settings);
        var directory = Path.GetDirectoryName(_settingsPath);
        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }

        var json = JsonSerializer.Serialize(normalized, new JsonSerializerOptions
        {
            WriteIndented = true
        });

        File.WriteAllText(_settingsPath, json);
    }

    private static ApplicationSettingsModel CreateDefault()
    {
        return new ApplicationSettingsModel
        {
            ExpirationWarningDays = DefaultExpirationWarningDays
        };
    }

    private static ApplicationSettingsModel Normalize(ApplicationSettingsModel? settings)
    {
        var warningDays = settings?.ExpirationWarningDays ?? DefaultExpirationWarningDays;
        warningDays = Math.Clamp(warningDays, 1, 90);

        return new ApplicationSettingsModel
        {
            ExpirationWarningDays = warningDays
        };
    }
}

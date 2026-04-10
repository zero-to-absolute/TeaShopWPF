using TeaShopManager.DataAccess;
using TeaShopManager.Models;

namespace TeaShopManager.Tests;

[TestClass]
public sealed class ApplicationSettingsStoreTests
{
    private string _settingsDirectory = string.Empty;
    private string _settingsPath = string.Empty;

    [TestInitialize]
    public void Setup()
    {
        _settingsDirectory = Path.Combine(Path.GetTempPath(), "TeaShopManagerTests", Guid.NewGuid().ToString("N"));
        _settingsPath = Path.Combine(_settingsDirectory, "user-settings.json");
    }

    [TestCleanup]
    public void Cleanup()
    {
        if (Directory.Exists(_settingsDirectory))
        {
            Directory.Delete(_settingsDirectory, true);
        }
    }

    [TestMethod]
    public void Load_WhenFileDoesNotExist_ReturnsDefaultSettings()
    {
        var store = new ApplicationSettingsStore(_settingsPath);

        var settings = store.Load();

        Assert.AreEqual(14, settings.ExpirationWarningDays);
    }

    [TestMethod]
    public void Save_ThenLoad_ClampsExpirationWarningDaysToAllowedRange()
    {
        var store = new ApplicationSettingsStore(_settingsPath);

        store.Save(new ApplicationSettingsModel
        {
            ExpirationWarningDays = 120
        });

        var settings = store.Load();

        Assert.AreEqual(90, settings.ExpirationWarningDays);
    }

    [TestMethod]
    public void Load_WhenJsonIsInvalid_ReturnsDefaultSettings()
    {
        Directory.CreateDirectory(_settingsDirectory);
        File.WriteAllText(_settingsPath, "{ invalid json }");

        var store = new ApplicationSettingsStore(_settingsPath);
        var settings = store.Load();

        Assert.AreEqual(14, settings.ExpirationWarningDays);
    }
}

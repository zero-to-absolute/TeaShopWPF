using TeaShopManager.DataAccess;

namespace TeaShopManager.Tests;

[TestClass]
public sealed class PasswordHasherTests
{
    [TestMethod]
    public void HashPasswordSha256_SamePassword_ReturnsSameHash()
    {
        const string password = "TeaShop123!";

        var firstHash = PasswordHasher.HashPasswordSha256(password);
        var secondHash = PasswordHasher.HashPasswordSha256(password);

        Assert.AreEqual(firstHash, secondHash);
    }

    [TestMethod]
    public void HashPasswordSha256_DifferentPasswords_ReturnDifferentHashes()
    {
        var firstHash = PasswordHasher.HashPasswordSha256("TeaShop123!");
        var secondHash = PasswordHasher.HashPasswordSha256("TeaShop123?");

        Assert.AreNotEqual(firstHash, secondHash);
    }

    [TestMethod]
    public void HashPasswordSha256_KnownPassword_ReturnsExpectedHash()
    {
        const string expectedHash = "8C6976E5B5410415BDE908BD4DEE15DFB167A9C873FC4BB8A81F6F2AB448A918";

        var actualHash = PasswordHasher.HashPasswordSha256("admin");

        Assert.AreEqual(expectedHash, actualHash);
    }
}

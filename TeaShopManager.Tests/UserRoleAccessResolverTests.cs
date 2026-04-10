using TeaShopManager.Models;

namespace TeaShopManager.Tests;

[TestClass]
public sealed class UserRoleAccessResolverTests
{
    [TestMethod]
    public void Resolve_AdminDirectorRole_ReturnsCombinedFlags()
    {
        var access = UserRoleAccessResolver.Resolve("Администратор/Директор");

        Assert.IsTrue(access.HasAccess(UserRoleAccess.Administrator));
        Assert.IsTrue(access.HasAccess(UserRoleAccess.Director));
        Assert.IsFalse(access.HasAccess(UserRoleAccess.Manager));
    }

    [TestMethod]
    public void Resolve_ManagerRole_IgnoresCaseAndWhitespace()
    {
        var access = UserRoleAccessResolver.Resolve("   меНеДжер   ");

        Assert.AreEqual(UserRoleAccess.Manager, access);
    }

    [TestMethod]
    public void Resolve_EmptyRole_ReturnsNone()
    {
        var access = UserRoleAccessResolver.Resolve(" ");

        Assert.AreEqual(UserRoleAccess.None, access);
    }
}

using System;

namespace TeaShopManager.Models;

[Flags]
public enum UserRoleAccess
{
    None = 0,
    Administrator = 1,
    Director = 2,
    Manager = 4,
    Seller = 8,
    Accountant = 16
}

public static class UserRoleAccessResolver
{
    public static UserRoleAccess Resolve(string? roleName)
    {
        if (string.IsNullOrWhiteSpace(roleName))
        {
            return UserRoleAccess.None;
        }

        var normalizedRole = roleName.Trim().ToLowerInvariant();
        var access = UserRoleAccess.None;

        if (normalizedRole.Contains("администратор"))
        {
            access |= UserRoleAccess.Administrator;
        }

        if (normalizedRole.Contains("директор"))
        {
            access |= UserRoleAccess.Director;
        }

        if (normalizedRole.Contains("менедж"))
        {
            access |= UserRoleAccess.Manager;
        }

        if (normalizedRole.Contains("продав"))
        {
            access |= UserRoleAccess.Seller;
        }

        if (normalizedRole.Contains("бухгалтер"))
        {
            access |= UserRoleAccess.Accountant;
        }

        return access;
    }

    public static bool HasAccess(this UserRoleAccess currentAccess, UserRoleAccess requiredAccess)
    {
        return (currentAccess & requiredAccess) == requiredAccess;
    }
}

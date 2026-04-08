using System.Data;
using Microsoft.Data.SqlClient;
using TeaShopManager.Models;

namespace TeaShopManager.DataAccess;

public sealed class AuthenticationRepository
{
    private readonly string _connectionString;

    public AuthenticationRepository(string connectionString)
    {
        _connectionString = connectionString;
    }

    public UserSession? Authenticate(string login, string password)
    {
        const string sql =
            """
            SELECT TOP (1)
                u.ID_USER,
                u.LOGIN,
                u.PASSWORD,
                u.FULL_NAME,
                r.ROLE_NAME
            FROM USERS u
            INNER JOIN ROLE r ON r.ID_ROLE = u.ID_ROLE
            WHERE u.LOGIN = @login;
            """;

        using var connection = new SqlConnection(_connectionString);
        using var command = new SqlCommand(sql, connection);
        command.Parameters.Add("@login", SqlDbType.VarChar, 50).Value = login;

        connection.Open();

        using var reader = command.ExecuteReader();
        if (!reader.Read())
        {
            return null;
        }

        var storedHash = reader["PASSWORD"]?.ToString()?.Trim();
        var incomingHash = PasswordHasher.HashPasswordSha256(password);

        if (!string.Equals(storedHash, incomingHash, StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        return new UserSession
        {
            UserId = Convert.ToInt32(reader["ID_USER"]),
            Login = reader["LOGIN"]?.ToString() ?? string.Empty,
            FullName = reader["FULL_NAME"]?.ToString() ?? string.Empty,
            RoleName = reader["ROLE_NAME"]?.ToString() ?? string.Empty
        };
    }
}

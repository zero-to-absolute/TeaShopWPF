using System.Data;
using Microsoft.Data.SqlClient;
using TeaShopManager.Models;

namespace TeaShopManager.DataAccess;

public sealed partial class TeaShopRepository
{
    public void SaveUser(UserEditorModel model)
    {
        using var connection = OpenConnection();

        if (model.UserId == 0)
        {
            const string insertSql = """
                                     INSERT INTO USERS (LOGIN, PASSWORD, FULL_NAME, ID_ROLE)
                                     VALUES (@login, @password, @fullName, @roleId);
                                     """;

            using var command = new SqlCommand(insertSql, connection);
            command.Parameters.Add("@login", SqlDbType.VarChar, 50).Value = model.Login.Trim();
            command.Parameters.Add("@password", SqlDbType.VarChar, 4000).Value =
                PasswordHasher.HashPasswordSha256(model.Password);
            AddNullableString(command, "@fullName", model.FullName, 4000);
            command.Parameters.Add("@roleId", SqlDbType.SmallInt).Value = model.RoleId;
            command.ExecuteNonQuery();
            return;
        }

        var updateSql = string.IsNullOrWhiteSpace(model.Password)
            ? """
              UPDATE USERS
              SET LOGIN = @login,
                  FULL_NAME = @fullName,
                  ID_ROLE = @roleId
              WHERE ID_USER = @userId;
              """
            : """
              UPDATE USERS
              SET LOGIN = @login,
                  PASSWORD = @password,
                  FULL_NAME = @fullName,
                  ID_ROLE = @roleId
              WHERE ID_USER = @userId;
              """;

        using (var command = new SqlCommand(updateSql, connection))
        {
            command.Parameters.Add("@userId", SqlDbType.Int).Value = model.UserId;
            command.Parameters.Add("@login", SqlDbType.VarChar, 50).Value = model.Login.Trim();
            AddNullableString(command, "@fullName", model.FullName, 4000);
            command.Parameters.Add("@roleId", SqlDbType.SmallInt).Value = model.RoleId;

            if (!string.IsNullOrWhiteSpace(model.Password))
            {
                command.Parameters.Add("@password", SqlDbType.VarChar, 4000).Value =
                    PasswordHasher.HashPasswordSha256(model.Password);
            }

            command.ExecuteNonQuery();
        }
    }

    public void DeleteUser(int userId)
    {
        const string sql = "DELETE FROM USERS WHERE ID_USER = @userId;";
        ExecuteDelete(sql, ("@userId", SqlDbType.Int, userId));
    }

    public void SaveCategory(CategoryEditorModel model)
    {
        using var connection = OpenConnection();
        if (model.CategoryId == 0)
        {
            const string insertSql = "INSERT INTO CATEGORY (CATEGORY_NAME) VALUES (@name);";
            using var command = new SqlCommand(insertSql, connection);
            command.Parameters.Add("@name", SqlDbType.VarChar, 100).Value = model.CategoryName.Trim();
            command.ExecuteNonQuery();
            return;
        }

        const string updateSql = """
                                 UPDATE CATEGORY
                                 SET CATEGORY_NAME = @name
                                 WHERE ID_CATEGORY = @categoryId;
                                 """;
        using (var command = new SqlCommand(updateSql, connection))
        {
            command.Parameters.Add("@categoryId", SqlDbType.SmallInt).Value = model.CategoryId;
            command.Parameters.Add("@name", SqlDbType.VarChar, 100).Value = model.CategoryName.Trim();
            command.ExecuteNonQuery();
        }
    }

    public void DeleteCategory(short categoryId)
    {
        const string sql = "DELETE FROM CATEGORY WHERE ID_CATEGORY = @categoryId;";
        ExecuteDelete(sql, ("@categoryId", SqlDbType.SmallInt, categoryId));
    }

    public void SaveRole(RoleEditorModel model)
    {
        using var connection = OpenConnection();
        if (model.RoleId == 0)
        {
            const string insertSql = "INSERT INTO ROLE (ROLE_NAME) VALUES (@name);";
            using var command = new SqlCommand(insertSql, connection);
            command.Parameters.Add("@name", SqlDbType.VarChar, 50).Value = model.RoleName.Trim();
            command.ExecuteNonQuery();
            return;
        }

        const string updateSql = "UPDATE ROLE SET ROLE_NAME = @name WHERE ID_ROLE = @roleId;";
        using var updateCommand = new SqlCommand(updateSql, connection);
        updateCommand.Parameters.Add("@roleId", SqlDbType.SmallInt).Value = model.RoleId;
        updateCommand.Parameters.Add("@name", SqlDbType.VarChar, 50).Value = model.RoleName.Trim();
        updateCommand.ExecuteNonQuery();
    }

    public void DeleteRole(short roleId)
    {
        const string sql = "DELETE FROM ROLE WHERE ID_ROLE = @roleId;";
        ExecuteDelete(sql, ("@roleId", SqlDbType.SmallInt, roleId));
    }

    public void SaveSupplier(SupplierEditorModel model)
    {
        using var connection = OpenConnection();
        if (model.SupplierId == 0)
        {
            const string insertSql = """
                                     INSERT INTO SUPPLIERS (SUPPLIER_NAME, CONTACT_PERSON, PHONE, EMAIL, ADDRESS)
                                     VALUES (@name, @contact, @phone, @email, @address);
                                     """;
            using var command = new SqlCommand(insertSql, connection);
            command.Parameters.Add("@name", SqlDbType.VarChar, 4000).Value = model.SupplierName.Trim();
            command.Parameters.Add("@contact", SqlDbType.VarChar, 4000).Value = model.ContactPerson.Trim();
            command.Parameters.Add("@phone", SqlDbType.VarChar, 20).Value = model.Phone.Trim();
            AddNullableString(command, "@email", model.Email, 100);
            command.Parameters.Add("@address", SqlDbType.VarChar, 4000).Value = model.Address.Trim();
            command.ExecuteNonQuery();
            return;
        }

        const string updateSql = """
                                 UPDATE SUPPLIERS
                                 SET SUPPLIER_NAME = @name,
                                     CONTACT_PERSON = @contact,
                                     PHONE = @phone,
                                     EMAIL = @email,
                                     ADDRESS = @address
                                 WHERE ID_SUPPLIER = @supplierId;
                                 """;

        using (var command = new SqlCommand(updateSql, connection))
        {
            command.Parameters.Add("@supplierId", SqlDbType.Int).Value = model.SupplierId;
            command.Parameters.Add("@name", SqlDbType.VarChar, 4000).Value = model.SupplierName.Trim();
            command.Parameters.Add("@contact", SqlDbType.VarChar, 4000).Value = model.ContactPerson.Trim();
            command.Parameters.Add("@phone", SqlDbType.VarChar, 20).Value = model.Phone.Trim();
            AddNullableString(command, "@email", model.Email, 100);
            command.Parameters.Add("@address", SqlDbType.VarChar, 4000).Value = model.Address.Trim();
            command.ExecuteNonQuery();
        }
    }

    public void DeleteSupplier(int supplierId)
    {
        const string sql = "DELETE FROM SUPPLIERS WHERE ID_SUPPLIER = @supplierId;";
        ExecuteDelete(sql, ("@supplierId", SqlDbType.Int, supplierId));
    }

    public void SaveProduct(ProductEditorModel model)
    {
        using var connection = OpenConnection();
        if (model.ProductId == 0)
        {
            const string insertSql = """
                                     INSERT INTO PRODUCTS
                                         (PRODUCT_NAME, PRICE, MIN_STOCK_QUANTITY, DESCRIPTION, EXPIRATION_DATE, ID_CATEGORY)
                                     VALUES
                                         (@name, @price, @minStock, @description, @expirationDays, @categoryId);
                                     """;
            using var command = new SqlCommand(insertSql, connection);
            FillProductParameters(command, model);
            command.ExecuteNonQuery();
            SyncLowStockNotifications(connection);
            return;
        }

        const string updateSql = """
                                 UPDATE PRODUCTS
                                 SET PRODUCT_NAME = @name,
                                     PRICE = @price,
                                     MIN_STOCK_QUANTITY = @minStock,
                                     DESCRIPTION = @description,
                                     EXPIRATION_DATE = @expirationDays,
                                     ID_CATEGORY = @categoryId
                                 WHERE ID_PRODUCT = @productId;
                                 """;
        using (var command = new SqlCommand(updateSql, connection))
        {
            command.Parameters.Add("@productId", SqlDbType.Int).Value = model.ProductId;
            FillProductParameters(command, model);
            command.ExecuteNonQuery();
        }

        SyncLowStockNotifications(connection);
    }

    public void DeleteProduct(int productId)
    {
        using var connection = OpenConnection();
        const string sql = "DELETE FROM PRODUCTS WHERE ID_PRODUCT = @productId;";
        using var command = new SqlCommand(sql, connection);
        command.Parameters.Add("@productId", SqlDbType.Int).Value = productId;
        command.ExecuteNonQuery();
        SyncLowStockNotifications(connection);
    }

    public void SaveStatusOrder(StatusEditorModel model)
    {
        SaveStatus("STATUS_ORDER", model);
    }

    public void DeleteStatusOrder(int statusId)
    {
        DeleteStatus("STATUS_ORDER", statusId);
    }

    public void SaveStatusSupply(StatusEditorModel model)
    {
        SaveStatus("STATUS_SUPPLIES", model);
    }

    public void DeleteStatusSupply(int statusId)
    {
        DeleteStatus("STATUS_SUPPLIES", statusId);
    }

}

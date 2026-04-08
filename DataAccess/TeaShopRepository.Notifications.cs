using System.Data;
using Microsoft.Data.SqlClient;
using TeaShopManager.Models;

namespace TeaShopManager.DataAccess;

public sealed partial class TeaShopRepository
{
    public void SaveNotification(NotificationEditorModel model)
    {
        using var connection = OpenConnection();
        if (model.NotificationId == 0)
        {
            const string insertSql =
                """
                INSERT INTO NOTIFICATIONS (NOTIFICATION_TYPE, MESSAGE, CREATED_DATE, ID_PRODUCT, ID_USER, IS_READ)
                VALUES (@type, @message, @createdDate, @productId, @userId, @isRead);
                """;
            using var command = new SqlCommand(insertSql, connection);
            FillNotificationParameters(command, model);
            command.ExecuteNonQuery();
            return;
        }

        const string updateSql =
            """
            UPDATE NOTIFICATIONS
            SET NOTIFICATION_TYPE = @type,
                MESSAGE = @message,
                CREATED_DATE = @createdDate,
                ID_PRODUCT = @productId,
                ID_USER = @userId,
                IS_READ = @isRead
            WHERE ID_NOTIFICATION = @notificationId;
            """;
        using var updateCommand = new SqlCommand(updateSql, connection);
        updateCommand.Parameters.Add("@notificationId", SqlDbType.Int).Value = model.NotificationId;
        FillNotificationParameters(updateCommand, model);
        updateCommand.ExecuteNonQuery();
    }

    public void DeleteNotification(int notificationId)
    {
        const string sql = "DELETE FROM NOTIFICATIONS WHERE ID_NOTIFICATION = @notificationId;";
        ExecuteDelete(sql, ("@notificationId", SqlDbType.Int, notificationId));
    }

    public void MarkNotificationAsRead(int notificationId)
    {
        const string sql =
            """
            UPDATE NOTIFICATIONS
            SET IS_READ = 1
            WHERE ID_NOTIFICATION = @notificationId;
            """;

        ExecuteDelete(sql, ("@notificationId", SqlDbType.Int, notificationId));
    }

    public void MarkAllNotificationsAsRead()
    {
        const string sql =
            """
            UPDATE NOTIFICATIONS
            SET IS_READ = 1
            WHERE IS_READ = 0;
            """;

        using var connection = OpenConnection();
        using var command = new SqlCommand(sql, connection);
        command.ExecuteNonQuery();
    }

}

using System.Windows;
using TeaShopManager.Models;
using TeaShopManager.Views;

namespace TeaShopManager.Views.Sections;

public partial class NotificationsSectionView : SectionViewBase
{
    public NotificationsSectionView()
    {
        InitializeComponent();
    }

    private void AddNotificationButton_OnClick(object sender, RoutedEventArgs e)
    {
        ExecuteWithHandling(() =>
        {
            var vm = RequireViewModel();
            var dialog = new NotificationEditorWindow(new NotificationEditorModel(), vm.GetProductLookupItems(), vm.GetUserLookupItems()) { Owner = OwnerWindow };
            if (dialog.ShowDialog() == true)
            {
                vm.SaveNotification(dialog.ResultModel);
            }
        });
    }

    private void EditNotificationButton_OnClick(object sender, RoutedEventArgs e)
    {
        ExecuteWithHandling(() =>
        {
            var vm = RequireViewModel();
            if (((FrameworkElement)sender).Tag is not NotificationPreviewItem item) return;

            var dialog = new NotificationEditorWindow(new NotificationEditorModel
            {
                NotificationId = item.NotificationId,
                NotificationType = item.NotificationType,
                Message = item.Message,
                CreatedAt = item.CreatedAtValue,
                ProductId = item.ProductId,
                UserId = item.UserId,
                IsRead = item.IsRead
            }, vm.GetProductLookupItems(), vm.GetUserLookupItems()) { Owner = OwnerWindow };

            if (dialog.ShowDialog() == true)
            {
                vm.SaveNotification(dialog.ResultModel);
            }
        });
    }

    private void DeleteNotificationButton_OnClick(object sender, RoutedEventArgs e)
    {
        ExecuteWithHandling(() =>
        {
            var vm = RequireViewModel();
            if (((FrameworkElement)sender).Tag is not NotificationPreviewItem item) return;
            if (ConfirmDelete("Удалить уведомление?"))
            {
                vm.DeleteNotification(item.NotificationId);
            }
        });
    }

    private void MarkNotificationAsReadButton_OnClick(object sender, RoutedEventArgs e)
    {
        ExecuteWithHandling(() =>
        {
            var vm = RequireViewModel();
            if (((FrameworkElement)sender).Tag is not NotificationPreviewItem item || !item.CanMarkAsRead) return;
            vm.MarkNotificationAsRead(item.NotificationId);
        });
    }

    private void MarkAllNotificationsAsReadButton_OnClick(object sender, RoutedEventArgs e)
    {
        ExecuteWithHandling(() =>
        {
            var vm = RequireViewModel();
            vm.MarkAllNotificationsAsRead();
        });
    }
}

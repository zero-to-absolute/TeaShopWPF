using System.Windows;
using TeaShopManager.Models;
using TeaShopManager.Views;

namespace TeaShopManager.Views.Sections;

public partial class AdministrationSectionView : SectionViewBase
{
    public AdministrationSectionView()
    {
        InitializeComponent();
    }

    private void AddUserButton_OnClick(object sender, RoutedEventArgs e)
    {
        ExecuteWithHandling(() =>
        {
            var vm = RequireViewModel();
            var dialog = new UserEditorWindow(new UserEditorModel(), vm.GetRoleLookupItems()) { Owner = OwnerWindow };
            if (dialog.ShowDialog() == true)
            {
                vm.SaveUser(dialog.ResultModel);
            }
        });
    }

    private void EditUserButton_OnClick(object sender, RoutedEventArgs e)
    {
        ExecuteWithHandling(() =>
        {
            var vm = RequireViewModel();
            if (((FrameworkElement)sender).Tag is not UserPreviewItem item) return;

            var dialog = new UserEditorWindow(new UserEditorModel
            {
                UserId = item.UserId,
                Login = item.Login,
                FullName = item.FullName,
                RoleId = item.RoleId
            }, vm.GetRoleLookupItems()) { Owner = OwnerWindow };

            if (dialog.ShowDialog() == true)
            {
                vm.SaveUser(dialog.ResultModel);
            }
        });
    }

    private void DeleteUserButton_OnClick(object sender, RoutedEventArgs e)
    {
        ExecuteWithHandling(() =>
        {
            var vm = RequireViewModel();
            if (((FrameworkElement)sender).Tag is not UserPreviewItem item) return;
            if (item.UserId == vm.CurrentUser.UserId)
            {
                MessageBox.Show(OwnerWindow, "Нельзя удалить пользователя, под которым вы сейчас вошли.", "TeaShopManager", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (ConfirmDelete($"Удалить пользователя \"{item.Login}\"?"))
            {
                vm.DeleteUser(item.UserId);
            }
        });
    }

    private void AddRoleButton_OnClick(object sender, RoutedEventArgs e)
    {
        ExecuteWithHandling(() =>
        {
            var vm = RequireViewModel();
            var dialog = new SimpleNameEditorWindow("Новая роль", "Название роли", string.Empty) { Owner = OwnerWindow };
            if (dialog.ShowDialog() == true)
            {
                vm.SaveRole(new RoleEditorModel { RoleName = dialog.ResultValue });
            }
        });
    }

    private void EditRoleButton_OnClick(object sender, RoutedEventArgs e)
    {
        ExecuteWithHandling(() =>
        {
            var vm = RequireViewModel();
            if (((FrameworkElement)sender).Tag is not RolePreviewItem item) return;

            var dialog = new SimpleNameEditorWindow("Редактирование роли", "Название роли", item.RoleName) { Owner = OwnerWindow };
            if (dialog.ShowDialog() == true)
            {
                vm.SaveRole(new RoleEditorModel
                {
                    RoleId = item.RoleId,
                    RoleName = dialog.ResultValue
                });
            }
        });
    }

    private void DeleteRoleButton_OnClick(object sender, RoutedEventArgs e)
    {
        ExecuteWithHandling(() =>
        {
            var vm = RequireViewModel();
            if (((FrameworkElement)sender).Tag is not RolePreviewItem item) return;
            if (ConfirmDelete($"Удалить роль \"{item.RoleName}\"?"))
            {
                vm.DeleteRole(item.RoleId);
            }
        });
    }

    private void AddStatusOrderButton_OnClick(object sender, RoutedEventArgs e)
    {
        ExecuteWithHandling(() =>
        {
            var vm = RequireViewModel();
            var dialog = new SimpleNameEditorWindow("Новый статус заказа", "Название статуса", string.Empty) { Owner = OwnerWindow };
            if (dialog.ShowDialog() == true)
            {
                vm.SaveStatusOrder(new StatusEditorModel { StatusName = dialog.ResultValue });
            }
        });
    }

    private void EditStatusOrderButton_OnClick(object sender, RoutedEventArgs e)
    {
        ExecuteWithHandling(() =>
        {
            var vm = RequireViewModel();
            if (((FrameworkElement)sender).Tag is not StatusPreviewItem item) return;

            var dialog = new SimpleNameEditorWindow("Редактирование статуса заказа", "Название статуса", item.StatusName) { Owner = OwnerWindow };
            if (dialog.ShowDialog() == true)
            {
                vm.SaveStatusOrder(new StatusEditorModel
                {
                    StatusId = item.StatusId,
                    StatusName = dialog.ResultValue
                });
            }
        });
    }

    private void DeleteStatusOrderButton_OnClick(object sender, RoutedEventArgs e)
    {
        ExecuteWithHandling(() =>
        {
            var vm = RequireViewModel();
            if (((FrameworkElement)sender).Tag is not StatusPreviewItem item) return;
            if (ConfirmDelete($"Удалить статус заказа \"{item.StatusName}\"?"))
            {
                vm.DeleteStatusOrder(item.StatusId);
            }
        });
    }

    private void AddStatusSupplyButton_OnClick(object sender, RoutedEventArgs e)
    {
        ExecuteWithHandling(() =>
        {
            var vm = RequireViewModel();
            var dialog = new SimpleNameEditorWindow("Новый статус поставки", "Название статуса", string.Empty) { Owner = OwnerWindow };
            if (dialog.ShowDialog() == true)
            {
                vm.SaveStatusSupply(new StatusEditorModel { StatusName = dialog.ResultValue });
            }
        });
    }

    private void EditStatusSupplyButton_OnClick(object sender, RoutedEventArgs e)
    {
        ExecuteWithHandling(() =>
        {
            var vm = RequireViewModel();
            if (((FrameworkElement)sender).Tag is not StatusPreviewItem item) return;

            var dialog = new SimpleNameEditorWindow("Редактирование статуса поставки", "Название статуса", item.StatusName) { Owner = OwnerWindow };
            if (dialog.ShowDialog() == true)
            {
                vm.SaveStatusSupply(new StatusEditorModel
                {
                    StatusId = item.StatusId,
                    StatusName = dialog.ResultValue
                });
            }
        });
    }

    private void DeleteStatusSupplyButton_OnClick(object sender, RoutedEventArgs e)
    {
        ExecuteWithHandling(() =>
        {
            var vm = RequireViewModel();
            if (((FrameworkElement)sender).Tag is not StatusPreviewItem item) return;
            if (ConfirmDelete($"Удалить статус поставки \"{item.StatusName}\"?"))
            {
                vm.DeleteStatusSupply(item.StatusId);
            }
        });
    }
}

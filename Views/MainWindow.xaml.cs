using System.Windows;
using TeaShopManager.Models;
using TeaShopManager.ViewModels;

namespace TeaShopManager.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
    }

    private ShellViewModel? ViewModel => DataContext as ShellViewModel;

    private void AddUserButton_OnClick(object sender, RoutedEventArgs e)
    {
        ExecuteWithHandling(() =>
        {
            var vm = RequireViewModel();
            var dialog = new UserEditorWindow(new UserEditorModel(), vm.GetRoleLookupItems()) { Owner = this };
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
            }, vm.GetRoleLookupItems()) { Owner = this };

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
                MessageBox.Show("Нельзя удалить пользователя, под которым вы сейчас вошли.", "TeaShopManager", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (ConfirmDelete($"Удалить пользователя \"{item.Login}\"?"))
            {
                vm.DeleteUser(item.UserId);
            }
        });
    }

    private void AddCategoryButton_OnClick(object sender, RoutedEventArgs e)
    {
        ExecuteWithHandling(() =>
        {
            var vm = RequireViewModel();
            var dialog = new CategoryEditorWindow(new CategoryEditorModel()) { Owner = this };
            if (dialog.ShowDialog() == true)
            {
                vm.SaveCategory(dialog.ResultModel);
            }
        });
    }

    private void EditCategoryButton_OnClick(object sender, RoutedEventArgs e)
    {
        ExecuteWithHandling(() =>
        {
            var vm = RequireViewModel();
            if (((FrameworkElement)sender).Tag is not CategoryPreviewItem item) return;

            var dialog = new CategoryEditorWindow(new CategoryEditorModel
            {
                CategoryId = item.CategoryId,
                CategoryName = item.CategoryName
            }) { Owner = this };

            if (dialog.ShowDialog() == true)
            {
                vm.SaveCategory(dialog.ResultModel);
            }
        });
    }

    private void DeleteCategoryButton_OnClick(object sender, RoutedEventArgs e)
    {
        ExecuteWithHandling(() =>
        {
            var vm = RequireViewModel();
            if (((FrameworkElement)sender).Tag is not CategoryPreviewItem item) return;
            if (ConfirmDelete($"Удалить категорию \"{item.CategoryName}\"?"))
            {
                vm.DeleteCategory(item.CategoryId);
            }
        });
    }

    private void AddSupplierButton_OnClick(object sender, RoutedEventArgs e)
    {
        ExecuteWithHandling(() =>
        {
            var vm = RequireViewModel();
            var dialog = new SupplierEditorWindow(new SupplierEditorModel()) { Owner = this };
            if (dialog.ShowDialog() == true)
            {
                vm.SaveSupplier(dialog.ResultModel);
            }
        });
    }

    private void EditSupplierButton_OnClick(object sender, RoutedEventArgs e)
    {
        ExecuteWithHandling(() =>
        {
            var vm = RequireViewModel();
            if (((FrameworkElement)sender).Tag is not SupplierPreviewItem item) return;

            var dialog = new SupplierEditorWindow(new SupplierEditorModel
            {
                SupplierId = item.SupplierId,
                SupplierName = item.SupplierName,
                ContactPerson = item.ContactPerson,
                Phone = item.Phone,
                Email = item.Email,
                Address = item.Address
            }) { Owner = this };

            if (dialog.ShowDialog() == true)
            {
                vm.SaveSupplier(dialog.ResultModel);
            }
        });
    }

    private void DeleteSupplierButton_OnClick(object sender, RoutedEventArgs e)
    {
        ExecuteWithHandling(() =>
        {
            var vm = RequireViewModel();
            if (((FrameworkElement)sender).Tag is not SupplierPreviewItem item) return;
            if (ConfirmDelete($"Удалить поставщика \"{item.SupplierName}\"?"))
            {
                vm.DeleteSupplier(item.SupplierId);
            }
        });
    }

    private void AddProductButton_OnClick(object sender, RoutedEventArgs e)
    {
        ExecuteWithHandling(() =>
        {
            var vm = RequireViewModel();
            var dialog = new ProductEditorWindow(new ProductEditorModel(), vm.GetCategoryLookupItems()) { Owner = this };
            if (dialog.ShowDialog() == true)
            {
                vm.SaveProduct(dialog.ResultModel);
            }
        });
    }

    private void EditProductButton_OnClick(object sender, RoutedEventArgs e)
    {
        ExecuteWithHandling(() =>
        {
            var vm = RequireViewModel();
            if (((FrameworkElement)sender).Tag is not ProductPreviewItem item) return;

            var dialog = new ProductEditorWindow(new ProductEditorModel
            {
                ProductId = item.ProductId,
                ProductName = item.ProductName,
                Price = item.PriceValue,
                MinStockQuantity = item.MinStockQuantityValue,
                Description = item.Description ?? string.Empty,
                ExpirationDays = item.ExpirationDays,
                CategoryId = item.CategoryId
            }, vm.GetCategoryLookupItems()) { Owner = this };

            if (dialog.ShowDialog() == true)
            {
                vm.SaveProduct(dialog.ResultModel);
            }
        });
    }

    private void DeleteProductButton_OnClick(object sender, RoutedEventArgs e)
    {
        ExecuteWithHandling(() =>
        {
            var vm = RequireViewModel();
            if (((FrameworkElement)sender).Tag is not ProductPreviewItem item) return;
            if (ConfirmDelete($"Удалить товар \"{item.ProductName}\"?"))
            {
                vm.DeleteProduct(item.ProductId);
            }
        });
    }

    private void AddRoleButton_OnClick(object sender, RoutedEventArgs e)
    {
        ExecuteWithHandling(() =>
        {
            var vm = RequireViewModel();
            var dialog = new SimpleNameEditorWindow("Новая роль", "Название роли", string.Empty) { Owner = this };
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

            var dialog = new SimpleNameEditorWindow("Редактирование роли", "Название роли", item.RoleName) { Owner = this };
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
            var dialog = new SimpleNameEditorWindow("Новый статус заказа", "Название статуса", string.Empty) { Owner = this };
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

            var dialog = new SimpleNameEditorWindow("Редактирование статуса заказа", "Название статуса", item.StatusName) { Owner = this };
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
            var dialog = new SimpleNameEditorWindow("Новый статус поставки", "Название статуса", string.Empty) { Owner = this };
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

            var dialog = new SimpleNameEditorWindow("Редактирование статуса поставки", "Название статуса", item.StatusName) { Owner = this };
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

    private void AddOrderButton_OnClick(object sender, RoutedEventArgs e)
    {
        ExecuteWithHandling(() =>
        {
            var vm = RequireViewModel();
            var dialog = new OrderEditorWindow(new OrderEditorModel(), vm.GetStatusOrderLookupItems(), vm.GetUserLookupItems()) { Owner = this };
            if (dialog.ShowDialog() == true)
            {
                vm.SaveOrder(dialog.ResultModel);
            }
        });
    }

    private void EditOrderButton_OnClick(object sender, RoutedEventArgs e)
    {
        ExecuteWithHandling(() =>
        {
            var vm = RequireViewModel();
            if (((FrameworkElement)sender).Tag is not OrderPreviewItem item) return;

            var dialog = new OrderEditorWindow(new OrderEditorModel
            {
                OrderId = item.OrderId,
                OrderDate = item.OrderDateValue,
                StatusId = item.StatusId,
                UserId = item.UserId
            }, vm.GetStatusOrderLookupItems(), vm.GetUserLookupItems()) { Owner = this };

            if (dialog.ShowDialog() == true)
            {
                vm.SaveOrder(dialog.ResultModel);
            }
        });
    }

    private void DeleteOrderButton_OnClick(object sender, RoutedEventArgs e)
    {
        ExecuteWithHandling(() =>
        {
            var vm = RequireViewModel();
            if (((FrameworkElement)sender).Tag is not OrderPreviewItem item) return;
            if (ConfirmDelete($"Удалить заказ {item.OrderCode}?"))
            {
                vm.DeleteOrder(item.OrderId);
            }
        });
    }

    private void AddOrderDetailButton_OnClick(object sender, RoutedEventArgs e)
    {
        ExecuteWithHandling(() =>
        {
            var vm = RequireViewModel();
            var dialog = new OrderDetailEditorWindow(new OrderDetailEditorModel(), vm.GetOrderLookupItems(), vm.GetProductLookupItems()) { Owner = this };
            if (dialog.ShowDialog() == true)
            {
                vm.SaveOrderDetail(dialog.ResultModel);
            }
        });
    }

    private void EditOrderDetailButton_OnClick(object sender, RoutedEventArgs e)
    {
        ExecuteWithHandling(() =>
        {
            var vm = RequireViewModel();
            if (((FrameworkElement)sender).Tag is not OrderDetailPreviewItem item) return;

            var dialog = new OrderDetailEditorWindow(new OrderDetailEditorModel
            {
                OriginalOrderId = item.OrderId,
                OriginalProductId = item.ProductId,
                OrderId = item.OrderId,
                ProductId = item.ProductId,
                Quantity = item.QuantityValue,
                Price = item.PriceValue
            }, vm.GetOrderLookupItems(), vm.GetProductLookupItems()) { Owner = this };

            if (dialog.ShowDialog() == true)
            {
                vm.SaveOrderDetail(dialog.ResultModel);
            }
        });
    }

    private void DeleteOrderDetailButton_OnClick(object sender, RoutedEventArgs e)
    {
        ExecuteWithHandling(() =>
        {
            var vm = RequireViewModel();
            if (((FrameworkElement)sender).Tag is not OrderDetailPreviewItem item) return;
            if (ConfirmDelete($"Удалить позицию {item.ProductName} из заказа {item.OrderCode}?"))
            {
                vm.DeleteOrderDetail(item.OrderId, item.ProductId);
            }
        });
    }

    private void AddNotificationButton_OnClick(object sender, RoutedEventArgs e)
    {
        ExecuteWithHandling(() =>
        {
            var vm = RequireViewModel();
            var dialog = new NotificationEditorWindow(new NotificationEditorModel(), vm.GetProductLookupItems(), vm.GetUserLookupItems()) { Owner = this };
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
            }, vm.GetProductLookupItems(), vm.GetUserLookupItems()) { Owner = this };

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

    private void AddSupplyButton_OnClick(object sender, RoutedEventArgs e)
    {
        ExecuteWithHandling(() =>
        {
            var vm = RequireViewModel();
            var dialog = new SupplyEditorWindow(new SupplyEditorModel(), vm.GetStatusSupplyLookupItems(), vm.GetSupplierLookupItems(), vm.GetUserLookupItems()) { Owner = this };
            if (dialog.ShowDialog() == true)
            {
                vm.SaveSupply(dialog.ResultModel);
            }
        });
    }

    private void EditSupplyButton_OnClick(object sender, RoutedEventArgs e)
    {
        ExecuteWithHandling(() =>
        {
            var vm = RequireViewModel();
            if (((FrameworkElement)sender).Tag is not SupplyPreviewItem item) return;

            var dialog = new SupplyEditorWindow(new SupplyEditorModel
            {
                SupplyId = item.SupplyId,
                CreationDate = item.CreationDateValue,
                DeliveryDate = item.DeliveryDateValue,
                StatusId = item.StatusId,
                SupplierId = item.SupplierId,
                UserId = item.UserId
            }, vm.GetStatusSupplyLookupItems(), vm.GetSupplierLookupItems(), vm.GetUserLookupItems()) { Owner = this };

            if (dialog.ShowDialog() == true)
            {
                vm.SaveSupply(dialog.ResultModel);
            }
        });
    }

    private void DeleteSupplyButton_OnClick(object sender, RoutedEventArgs e)
    {
        ExecuteWithHandling(() =>
        {
            var vm = RequireViewModel();
            if (((FrameworkElement)sender).Tag is not SupplyPreviewItem item) return;
            if (ConfirmDelete($"Удалить поставку {item.SupplyCode}?"))
            {
                vm.DeleteSupply(item.SupplyId);
            }
        });
    }

    private void AddSupplyDetailButton_OnClick(object sender, RoutedEventArgs e)
    {
        ExecuteWithHandling(() =>
        {
            var vm = RequireViewModel();
            var dialog = new SupplyDetailEditorWindow(new SupplyDetailEditorModel(), vm.GetSupplyLookupItems(), vm.GetProductLookupItems()) { Owner = this };
            if (dialog.ShowDialog() == true)
            {
                vm.SaveSupplyDetail(dialog.ResultModel);
            }
        });
    }

    private void EditSupplyDetailButton_OnClick(object sender, RoutedEventArgs e)
    {
        ExecuteWithHandling(() =>
        {
            var vm = RequireViewModel();
            if (((FrameworkElement)sender).Tag is not SupplyDetailPreviewItem item) return;

            var dialog = new SupplyDetailEditorWindow(new SupplyDetailEditorModel
            {
                OriginalSupplyId = item.SupplyId,
                OriginalProductId = item.ProductId,
                SupplyId = item.SupplyId,
                ProductId = item.ProductId,
                Quantity = item.QuantityValue,
                UnitPrice = item.UnitPriceValue,
                ProductionDate = item.ProductionDateValue
            }, vm.GetSupplyLookupItems(), vm.GetProductLookupItems()) { Owner = this };

            if (dialog.ShowDialog() == true)
            {
                vm.SaveSupplyDetail(dialog.ResultModel);
            }
        });
    }

    private void DeleteSupplyDetailButton_OnClick(object sender, RoutedEventArgs e)
    {
        ExecuteWithHandling(() =>
        {
            var vm = RequireViewModel();
            if (((FrameworkElement)sender).Tag is not SupplyDetailPreviewItem item) return;
            if (ConfirmDelete($"Удалить позицию {item.ProductName} из поставки {item.SupplyCode}?"))
            {
                vm.DeleteSupplyDetail(item.SupplyId, item.ProductId);
            }
        });
    }

    private void AddLossButton_OnClick(object sender, RoutedEventArgs e)
    {
        ExecuteWithHandling(() =>
        {
            var vm = RequireViewModel();
            var dialog = new LossEditorWindow(new LossEditorModel(), vm.GetProductLookupItems()) { Owner = this };
            if (dialog.ShowDialog() == true)
            {
                vm.SaveLoss(dialog.ResultModel);
            }
        });
    }

    private void EditLossButton_OnClick(object sender, RoutedEventArgs e)
    {
        ExecuteWithHandling(() =>
        {
            var vm = RequireViewModel();
            if (((FrameworkElement)sender).Tag is not LossPreviewItem item) return;

            var dialog = new LossEditorWindow(new LossEditorModel
            {
                LossId = item.LossId,
                ProductId = item.ProductId,
                Reason = item.Reason,
                LossDate = item.LossDateValue
            }, vm.GetProductLookupItems()) { Owner = this };

            if (dialog.ShowDialog() == true)
            {
                vm.SaveLoss(dialog.ResultModel);
            }
        });
    }

    private void DeleteLossButton_OnClick(object sender, RoutedEventArgs e)
    {
        ExecuteWithHandling(() =>
        {
            var vm = RequireViewModel();
            if (((FrameworkElement)sender).Tag is not LossPreviewItem item) return;
            if (ConfirmDelete($"Удалить списание {item.LossCode}?"))
            {
                vm.DeleteLoss(item.LossId);
            }
        });
    }

    private void AddSaleLineButton_OnClick(object sender, RoutedEventArgs e)
    {
        ExecuteWithHandling(() =>
        {
            var vm = RequireViewModel();
            var dialog = new SaleLineWindow(vm.GetProductsForSale()) { Owner = this };
            if (dialog.ShowDialog() != true) return;

            var product = vm.GetProductById(dialog.SelectedProductId);
            if (product is null) return;
            vm.AddSaleLine(product, dialog.Quantity);
        });
    }

    private void QuickSaleButton_OnClick(object sender, RoutedEventArgs e)
    {
        ExecuteWithHandling(() =>
        {
            var vm = RequireViewModel();
            if (((FrameworkElement)sender).Tag is not QuickSaleItem item || item.ProductId is null) return;
            var product = vm.GetProductById(item.ProductId.Value);
            if (product is null) return;
            vm.AddSaleLine(product, 1);
        });
    }

    private void RemoveSaleLineButton_OnClick(object sender, RoutedEventArgs e)
    {
        var vm = ViewModel;
        if (vm is null || ((FrameworkElement)sender).Tag is not SaleLinePreviewItem item) return;
        vm.RemoveSaleLine(item.ProductId);
    }

    private void CancelSaleButton_OnClick(object sender, RoutedEventArgs e)
    {
        var vm = ViewModel;
        if (vm is null) return;
        if (!vm.SaleLines.Any() || MessageBox.Show("Очистить текущую продажу?", "TeaShopManager", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
        {
            vm.CancelCurrentSale();
        }
    }

    private void CompleteSaleButton_OnClick(object sender, RoutedEventArgs e)
    {
        ExecuteWithHandling(() =>
        {
            var vm = RequireViewModel();
            if (!vm.SaleLines.Any())
            {
                MessageBox.Show("Добавьте хотя бы одну позицию в продажу.", "TeaShopManager", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var orderId = vm.CompleteCurrentSale();
            MessageBox.Show($"Продажа сохранена. Заказ #{orderId} создан.", "TeaShopManager", MessageBoxButton.OK, MessageBoxImage.Information);
        });
    }

    private bool ConfirmDelete(string message)
    {
        return MessageBox.Show(message, "TeaShopManager", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes;
    }

    private ShellViewModel RequireViewModel()
    {
        return ViewModel ?? throw new InvalidOperationException("DataContext окна не инициализирован.");
    }

    private void ExecuteWithHandling(Action action)
    {
        try
        {
            action();
        }
        catch (Exception exception)
        {
            MessageBox.Show(exception.Message, "TeaShopManager", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
}

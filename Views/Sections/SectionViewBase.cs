using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using TeaShopManager.Models;
using TeaShopManager.ViewModels;
using TeaShopManager.Views;

namespace TeaShopManager.Views.Sections;

public abstract class SectionViewBase : UserControl
{
    protected ShellViewModel? ViewModel => DataContext as ShellViewModel;

    protected ShellViewModel RequireViewModel()
    {
        return ViewModel ?? throw new InvalidOperationException("DataContext секции не инициализирован.");
    }

    protected Window OwnerWindow =>
        Window.GetWindow(this) ?? throw new InvalidOperationException("Окно-владелец секции не найден.");

    protected bool ConfirmDelete(string message)
    {
        return MessageBox.Show(OwnerWindow, message, "TeaShopManager", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes;
    }

    protected void ExecuteWithHandling(Action action)
    {
        try
        {
            action();
        }
        catch (Exception exception)
        {
            MessageBox.Show(OwnerWindow, exception.Message, "TeaShopManager", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    protected void ShowProductMovementsDialog(ProductPreviewItem product)
    {
        var vm = RequireViewModel();

        while (true)
        {
            var currentProduct = vm.GetProductById(product.ProductId) ?? product;
            var movementsWindow = new ProductMovementsWindow(currentProduct, vm.GetProductMovements(currentProduct.ProductId))
            {
                Owner = OwnerWindow
            };
            movementsWindow.ShowDialog();

            if (!movementsWindow.RequestCustomerReturn)
            {
                return;
            }

            var returnDialog = new CustomerReturnWindow(vm.GetCustomerReturnDraft(currentProduct.ProductId))
            {
                Owner = OwnerWindow
            };
            if (returnDialog.ShowDialog() != true)
            {
                continue;
            }

            vm.CreateCustomerReturn(returnDialog.ResultModel);
            MessageBox.Show(
                OwnerWindow,
                $"Возврат товара \"{currentProduct.ProductName}\" сохранён и добавлен в FIFO как новое входящее движение.",
                "TeaShopManager",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }
    }

    protected static T? FindVisualParent<T>(DependencyObject? child) where T : DependencyObject
    {
        while (child is not null)
        {
            if (child is T target)
            {
                return target;
            }

            child = VisualTreeHelper.GetParent(child);
        }

        return null;
    }
}

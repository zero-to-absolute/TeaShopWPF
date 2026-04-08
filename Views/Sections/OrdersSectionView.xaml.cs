using System.Windows;
using TeaShopManager.Models;
using TeaShopManager.Views;

namespace TeaShopManager.Views.Sections;

public partial class OrdersSectionView : SectionViewBase
{
    public OrdersSectionView()
    {
        InitializeComponent();
    }

    private void AddOrderButton_OnClick(object sender, RoutedEventArgs e)
    {
        ExecuteWithHandling(() =>
        {
            var vm = RequireViewModel();
            var dialog = new OrderEditorWindow(new OrderEditorModel(), vm.GetStatusOrderLookupItems(), vm.GetUserLookupItems()) { Owner = OwnerWindow };
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
            }, vm.GetStatusOrderLookupItems(), vm.GetUserLookupItems()) { Owner = OwnerWindow };

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
            var dialog = new OrderDetailEditorWindow(new OrderDetailEditorModel(), vm.GetOrderLookupItems(), vm.GetProductLookupItems()) { Owner = OwnerWindow };
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
            }, vm.GetOrderLookupItems(), vm.GetProductLookupItems()) { Owner = OwnerWindow };

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
}

using System.Windows;
using TeaShopManager.Models;
using TeaShopManager.Views;

namespace TeaShopManager.Views.Sections;

public partial class SalesSectionView : SectionViewBase
{
    public SalesSectionView()
    {
        InitializeComponent();
    }

    private void AddSaleLineButton_OnClick(object sender, RoutedEventArgs e)
    {
        ExecuteWithHandling(() =>
        {
            var vm = RequireViewModel();
            var dialog = new SaleLineWindow(vm.GetProductsForSale()) { Owner = OwnerWindow };
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
        if (!vm.SaleLines.Any() || MessageBox.Show(OwnerWindow, "Очистить текущую продажу?", "TeaShopManager", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
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
                MessageBox.Show(OwnerWindow, "Добавьте хотя бы одну позицию в продажу.", "TeaShopManager", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var orderId = vm.CompleteCurrentSale();
            MessageBox.Show(OwnerWindow, $"Продажа сохранена. Заказ #{orderId} создан.", "TeaShopManager", MessageBoxButton.OK, MessageBoxImage.Information);
        });
    }
}

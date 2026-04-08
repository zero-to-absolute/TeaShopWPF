using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using TeaShopManager.Models;
using TeaShopManager.Views;

namespace TeaShopManager.Views.Sections;

public partial class CatalogSectionView : SectionViewBase
{
    public CatalogSectionView()
    {
        InitializeComponent();
    }

    private void AddCategoryButton_OnClick(object sender, RoutedEventArgs e)
    {
        ExecuteWithHandling(() =>
        {
            var vm = RequireViewModel();
            var dialog = new CategoryEditorWindow(new CategoryEditorModel()) { Owner = OwnerWindow };
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
            }) { Owner = OwnerWindow };

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
            var dialog = new SupplierEditorWindow(new SupplierEditorModel()) { Owner = OwnerWindow };
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
            }) { Owner = OwnerWindow };

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
            var dialog = new ProductEditorWindow(new ProductEditorModel(), vm.GetCategoryLookupItems()) { Owner = OwnerWindow };
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
            }, vm.GetCategoryLookupItems()) { Owner = OwnerWindow };

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

    private void ProductsDataGrid_OnMouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        ExecuteWithHandling(() =>
        {
            if (FindVisualParent<Button>(e.OriginalSource as DependencyObject) is not null)
            {
                return;
            }

            if (sender is not DataGrid { SelectedItem: ProductPreviewItem item })
            {
                return;
            }

            ShowProductMovementsDialog(item);
        });
    }

    private void ShowAllProductsTabButton_OnClick(object sender, RoutedEventArgs e)
    {
        ViewModel?.ShowAllProducts();
    }

    private void ShowCriticalProductsTabButton_OnClick(object sender, RoutedEventArgs e)
    {
        ViewModel?.ShowCriticalProducts();
    }
}

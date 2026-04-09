using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using TeaShopManager.Models;
using TeaShopManager.Views;

namespace TeaShopManager.Views.Sections;

public partial class InventorySectionView : SectionViewBase
{
    public InventorySectionView()
    {
        InitializeComponent();
    }

    private void AddSupplyButton_OnClick(object sender, RoutedEventArgs e)
    {
        ExecuteWithHandling(() =>
        {
            var vm = RequireViewModel();
            var dialog = new SupplyEditorWindow(new SupplyEditorModel(), vm.GetStatusSupplyLookupItems(), vm.GetSupplierLookupItems(), vm.GetUserLookupItems()) { Owner = OwnerWindow };
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
            }, vm.GetStatusSupplyLookupItems(), vm.GetSupplierLookupItems(), vm.GetUserLookupItems()) { Owner = OwnerWindow };

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

    private void ReceiveSupplyButton_OnClick(object sender, RoutedEventArgs e)
    {
        ExecuteWithHandling(() =>
        {
            var vm = RequireViewModel();
            if (((FrameworkElement)sender).Tag is not SupplyPreviewItem item || !item.CanReceive) return;
            if (!ConfirmDelete($"Принять поставку {item.SupplyCode}? После приёмки она начнёт учитываться в остатках."))
            {
                return;
            }

            vm.ReceiveSupply(item.SupplyId);
            MessageBox.Show(OwnerWindow, $"Поставка {item.SupplyCode} принята и учтена в складских остатках.", "TeaShopManager", MessageBoxButton.OK, MessageBoxImage.Information);
        });
    }

    private void AddSupplyDetailButton_OnClick(object sender, RoutedEventArgs e)
    {
        ExecuteWithHandling(() =>
        {
            var vm = RequireViewModel();
            var dialog = new SupplyDetailEditorWindow(new SupplyDetailEditorModel(), vm.GetSupplyLookupItems(), vm.GetProductLookupItems()) { Owner = OwnerWindow };
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
            }, vm.GetSupplyLookupItems(), vm.GetProductLookupItems()) { Owner = OwnerWindow };

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
            var dialog = new LossEditorWindow(new LossEditorModel(), vm.GetProductLookupItems()) { Owner = OwnerWindow };
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
                Quantity = item.QuantityValue,
                Reason = item.Reason,
                LossDate = item.LossDateValue
            }, vm.GetProductLookupItems()) { Owner = OwnerWindow };

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

    private void SuppliesDataGrid_OnMouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        ExecuteWithHandling(() =>
        {
            if (FindVisualParent<Button>(e.OriginalSource as DependencyObject) is not null)
            {
                return;
            }

            if (FindVisualParent<DataGridRow>(e.OriginalSource as DependencyObject) is null)
            {
                return;
            }

            if (sender is not DataGrid { SelectedItem: SupplyPreviewItem })
            {
                return;
            }

            RequireViewModel().NavigateToSection("SupplyDetails");
        });
    }
}

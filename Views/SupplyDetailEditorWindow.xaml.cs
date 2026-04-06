using System.Globalization;
using System.Windows;
using TeaShopManager.Models;

namespace TeaShopManager.Views;

public partial class SupplyDetailEditorWindow : Window
{
    public SupplyDetailEditorWindow(SupplyDetailEditorModel model, IReadOnlyList<LookupItem> supplyItems, IReadOnlyList<LookupItem> productItems)
    {
        InitializeComponent();
        MaxWidth = SystemParameters.WorkArea.Width - 40;
        MaxHeight = SystemParameters.WorkArea.Height - 40;
        Loaded += (_, _) => SizeToContent = System.Windows.SizeToContent.Manual;
        WindowTitle = model.OriginalSupplyId == 0 && model.OriginalProductId == 0 ? "Новая позиция поставки" : "Редактирование позиции поставки";
        SupplyItems = supplyItems;
        ProductItems = productItems;
        ResultModel = model;
        DataContext = this;

        SupplyComboBox.SelectedValue = model.SupplyId == 0 && supplyItems.Count > 0 ? supplyItems[0].Id : model.SupplyId;
        ProductComboBox.SelectedValue = model.ProductId == 0 && productItems.Count > 0 ? productItems[0].Id : model.ProductId;
        QuantityTextBox.Text = model.Quantity == 0 ? string.Empty : model.Quantity.ToString(CultureInfo.InvariantCulture);
        UnitPriceTextBox.Text = model.UnitPrice == 0 ? string.Empty : model.UnitPrice.ToString("0.##", CultureInfo.InvariantCulture);
        ProductionDatePicker.SelectedDate = model.ProductionDate == default ? DateTime.Today : model.ProductionDate;
    }

    public string WindowTitle { get; }
    public IReadOnlyList<LookupItem> SupplyItems { get; }
    public IReadOnlyList<LookupItem> ProductItems { get; }
    public SupplyDetailEditorModel ResultModel { get; }

    private void SaveButton_OnClick(object sender, RoutedEventArgs e)
    {
        if (SupplyComboBox.SelectedValue is null || ProductComboBox.SelectedValue is null || ProductionDatePicker.SelectedDate is null)
        {
            MessageBox.Show("Заполните данные позиции поставки.", "TeaShopManager", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (!int.TryParse(QuantityTextBox.Text, out var quantity) || quantity <= 0)
        {
            MessageBox.Show("Количество должно быть больше нуля.", "TeaShopManager", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (!decimal.TryParse(UnitPriceTextBox.Text.Replace(',', '.'), NumberStyles.Number, CultureInfo.InvariantCulture, out var unitPrice) || unitPrice <= 0)
        {
            MessageBox.Show("Цена за единицу должна быть больше нуля.", "TeaShopManager", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        ResultModel.SupplyId = Convert.ToInt32(SupplyComboBox.SelectedValue);
        ResultModel.ProductId = Convert.ToInt32(ProductComboBox.SelectedValue);
        ResultModel.Quantity = quantity;
        ResultModel.UnitPrice = unitPrice;
        ResultModel.ProductionDate = ProductionDatePicker.SelectedDate.Value;
        DialogResult = true;
    }

    private void CancelButton_OnClick(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
    }
}

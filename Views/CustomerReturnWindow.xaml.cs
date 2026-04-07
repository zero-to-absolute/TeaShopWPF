using System.Globalization;
using System.Windows;
using TeaShopManager.Models;

namespace TeaShopManager.Views;

public partial class CustomerReturnWindow : Window
{
    public CustomerReturnWindow(CustomerReturnEditorModel model)
    {
        InitializeComponent();
        MaxWidth = SystemParameters.WorkArea.Width - 40;
        MaxHeight = SystemParameters.WorkArea.Height - 40;
        Loaded += (_, _) => SizeToContent = System.Windows.SizeToContent.Manual;
        ResultModel = model;
        DataContext = this;

        ReturnDatePicker.SelectedDate = model.ReturnDate;
        QuantityTextBox.Text = model.Quantity <= 0 ? string.Empty : model.Quantity.ToString(CultureInfo.InvariantCulture);
        UnitPriceTextBox.Text = model.UnitPrice <= 0 ? string.Empty : model.UnitPrice.ToString("0.##", CultureInfo.InvariantCulture);
        ProductionDatePicker.SelectedDate = model.ProductionDate == default ? DateTime.Today : model.ProductionDate;
    }

    public CustomerReturnEditorModel ResultModel { get; }

    private void SaveButton_OnClick(object sender, RoutedEventArgs e)
    {
        if (ReturnDatePicker.SelectedDate is null || ProductionDatePicker.SelectedDate is null)
        {
            MessageBox.Show("Укажите дату возврата и дату производства.", "TeaShopManager", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (!int.TryParse(QuantityTextBox.Text, out var quantity) || quantity <= 0)
        {
            MessageBox.Show("Количество возврата должно быть больше нуля.", "TeaShopManager", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (!decimal.TryParse(UnitPriceTextBox.Text.Replace(',', '.'), NumberStyles.Number, CultureInfo.InvariantCulture, out var unitPrice) || unitPrice <= 0)
        {
            MessageBox.Show("Закупочная цена должна быть больше нуля.", "TeaShopManager", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        ResultModel.ReturnDate = ReturnDatePicker.SelectedDate.Value;
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

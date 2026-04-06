using System.Globalization;
using System.Windows;
using TeaShopManager.Models;

namespace TeaShopManager.Views;

public partial class OrderDetailEditorWindow : Window
{
    public OrderDetailEditorWindow(OrderDetailEditorModel model, IReadOnlyList<LookupItem> orderItems, IReadOnlyList<LookupItem> productItems)
    {
        InitializeComponent();
        MaxWidth = SystemParameters.WorkArea.Width - 40;
        MaxHeight = SystemParameters.WorkArea.Height - 40;
        Loaded += (_, _) => SizeToContent = System.Windows.SizeToContent.Manual;
        WindowTitle = model.OriginalOrderId == 0 && model.OriginalProductId == 0 ? "Новая позиция заказа" : "Редактирование позиции заказа";
        OrderItems = orderItems;
        ProductItems = productItems;
        ResultModel = model;
        DataContext = this;

        OrderComboBox.SelectedValue = model.OrderId == 0 && orderItems.Count > 0 ? orderItems[0].Id : model.OrderId;
        ProductComboBox.SelectedValue = model.ProductId == 0 && productItems.Count > 0 ? productItems[0].Id : model.ProductId;
        QuantityTextBox.Text = model.Quantity == 0 ? string.Empty : model.Quantity.ToString(CultureInfo.InvariantCulture);
        PriceTextBox.Text = model.Price == 0 ? string.Empty : model.Price.ToString("0.##", CultureInfo.InvariantCulture);
    }

    public string WindowTitle { get; }
    public IReadOnlyList<LookupItem> OrderItems { get; }
    public IReadOnlyList<LookupItem> ProductItems { get; }
    public OrderDetailEditorModel ResultModel { get; }

    private void SaveButton_OnClick(object sender, RoutedEventArgs e)
    {
        if (OrderComboBox.SelectedValue is null || ProductComboBox.SelectedValue is null)
        {
            MessageBox.Show("Выберите заказ и товар.", "TeaShopManager", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (!int.TryParse(QuantityTextBox.Text, out var quantity) || quantity <= 0)
        {
            MessageBox.Show("Количество должно быть больше нуля.", "TeaShopManager", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (!decimal.TryParse(PriceTextBox.Text.Replace(',', '.'), NumberStyles.Number, CultureInfo.InvariantCulture, out var price) || price <= 0)
        {
            MessageBox.Show("Цена должна быть больше нуля.", "TeaShopManager", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        ResultModel.OrderId = Convert.ToInt32(OrderComboBox.SelectedValue);
        ResultModel.ProductId = Convert.ToInt32(ProductComboBox.SelectedValue);
        ResultModel.Quantity = quantity;
        ResultModel.Price = price;
        DialogResult = true;
    }

    private void CancelButton_OnClick(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
    }
}

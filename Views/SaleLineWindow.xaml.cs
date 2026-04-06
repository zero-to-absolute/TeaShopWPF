using System.Windows;
using TeaShopManager.Models;

namespace TeaShopManager.Views;

public partial class SaleLineWindow : Window
{
    public SaleLineWindow(IReadOnlyList<ProductPreviewItem> products, int? selectedProductId = null)
    {
        InitializeComponent();
        MaxWidth = SystemParameters.WorkArea.Width - 40;
        MaxHeight = SystemParameters.WorkArea.Height - 40;
        Loaded += (_, _) => SizeToContent = System.Windows.SizeToContent.Manual;
        Products = products;
        DataContext = this;
        ProductComboBox.SelectedValue = selectedProductId ?? products.FirstOrDefault()?.ProductId;
        QuantityTextBox.Text = "1";
    }

    public IReadOnlyList<ProductPreviewItem> Products { get; }

    public int SelectedProductId { get; private set; }

    public int Quantity { get; private set; }

    private void SaveButton_OnClick(object sender, RoutedEventArgs e)
    {
        if (ProductComboBox.SelectedValue is null)
        {
            MessageBox.Show("Выберите товар.", "TeaShopManager", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (!int.TryParse(QuantityTextBox.Text, out var quantity) || quantity <= 0)
        {
            MessageBox.Show("Количество должно быть больше нуля.", "TeaShopManager", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        SelectedProductId = Convert.ToInt32(ProductComboBox.SelectedValue);
        Quantity = quantity;
        DialogResult = true;
    }

    private void CancelButton_OnClick(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
    }
}

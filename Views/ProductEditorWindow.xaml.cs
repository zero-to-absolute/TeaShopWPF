using System.Globalization;
using System.Windows;
using TeaShopManager.Models;

namespace TeaShopManager.Views;

public partial class ProductEditorWindow : Window
{
    public ProductEditorWindow(ProductEditorModel model, IReadOnlyList<LookupItem> categoryItems)
    {
        InitializeComponent();
        MaxWidth = SystemParameters.WorkArea.Width - 40;
        MaxHeight = SystemParameters.WorkArea.Height - 40;
        Loaded += (_, _) => SizeToContent = System.Windows.SizeToContent.Manual;
        WindowTitle = model.ProductId == 0 ? "Новый товар" : "Редактирование товара";
        CategoryItems = categoryItems;
        ResultModel = new ProductEditorModel
        {
            ProductId = model.ProductId,
            ProductName = model.ProductName,
            Price = model.Price,
            MinStockQuantity = model.MinStockQuantity,
            Description = model.Description,
            ExpirationDays = model.ExpirationDays,
            CategoryId = model.CategoryId
        };
        DataContext = this;

        ProductNameTextBox.Text = model.ProductName;
        PriceTextBox.Text = model.Price == 0 ? string.Empty : model.Price.ToString("0.##", CultureInfo.InvariantCulture);
        MinStockTextBox.Text = model.MinStockQuantity == 0 ? string.Empty : model.MinStockQuantity.ToString(CultureInfo.InvariantCulture);
        ExpirationTextBox.Text = model.ExpirationDays == 0 ? string.Empty : model.ExpirationDays.ToString(CultureInfo.InvariantCulture);
        DescriptionTextBox.Text = model.Description;
        CategoryComboBox.SelectedValue = model.CategoryId == 0 && categoryItems.Count > 0 ? categoryItems[0].Id : model.CategoryId;
    }

    public string WindowTitle { get; }

    public IReadOnlyList<LookupItem> CategoryItems { get; }

    public ProductEditorModel ResultModel { get; }

    private void SaveButton_OnClick(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(ProductNameTextBox.Text) || CategoryComboBox.SelectedValue is null)
        {
            MessageBox.Show("Заполните название товара и категорию.", "TeaShopManager", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (!decimal.TryParse(PriceTextBox.Text.Replace(',', '.'), NumberStyles.Number, CultureInfo.InvariantCulture, out var price) || price <= 0)
        {
            MessageBox.Show("Цена должна быть больше нуля.", "TeaShopManager", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (!int.TryParse(MinStockTextBox.Text, out var minStock) || minStock < 0)
        {
            MessageBox.Show("Минимальный остаток должен быть неотрицательным.", "TeaShopManager", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (!short.TryParse(ExpirationTextBox.Text, out var expirationDays) || expirationDays <= 0)
        {
            MessageBox.Show("Срок годности должен быть больше нуля.", "TeaShopManager", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        ResultModel.ProductName = ProductNameTextBox.Text.Trim();
        ResultModel.Price = price;
        ResultModel.MinStockQuantity = minStock;
        ResultModel.ExpirationDays = expirationDays;
        ResultModel.Description = DescriptionTextBox.Text.Trim();
        ResultModel.CategoryId = Convert.ToInt16(CategoryComboBox.SelectedValue);
        DialogResult = true;
    }

    private void CancelButton_OnClick(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
    }
}

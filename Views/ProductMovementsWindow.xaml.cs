using System.Collections.ObjectModel;
using System.Windows;
using TeaShopManager.Models;

namespace TeaShopManager.Views;

public partial class ProductMovementsWindow : Window
{
    public ProductMovementsWindow(ProductPreviewItem product, IReadOnlyList<ProductMovementPreviewItem> movementItems)
    {
        InitializeComponent();
        Product = product;
        MovementItems = new ObservableCollection<ProductMovementPreviewItem>(movementItems);
        CanCreateReturn = !string.Equals(product.CategoryName.Trim(), "Чай", StringComparison.CurrentCultureIgnoreCase);
        WindowTitle = $"Партии товара: {product.ProductName}";
        ProductCaption = $"Категория: {product.CategoryName}  •  Цена: {product.Price}  •  Остаток: {product.StockQuantity}";
        FifoCaption = MovementItems.Count == 0
            ? "Принятых партий пока нет."
            : "FIFO: сначала списываются самые ранние партии.";
        ReturnPolicyCaption = CanCreateReturn
            ? "Для этой категории доступен возврат на склад."
            : "Для категории \"Чай\" возврат на склад недоступен.";
        DataContext = this;
    }

    public ProductPreviewItem Product { get; }
    public ObservableCollection<ProductMovementPreviewItem> MovementItems { get; }
    public bool CanCreateReturn { get; }
    public string WindowTitle { get; }
    public string ProductCaption { get; }
    public string FifoCaption { get; }
    public string ReturnPolicyCaption { get; }
    public bool RequestCustomerReturn { get; private set; }

    private void CreateReturnButton_OnClick(object sender, RoutedEventArgs e)
    {
        RequestCustomerReturn = true;
        Close();
    }

    private void CloseButton_OnClick(object sender, RoutedEventArgs e)
    {
        Close();
    }
}

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
        ProductCaption = $"Категория: {product.CategoryName}  •  Розничная цена: {product.Price}  •  Общий остаток: {product.StockQuantity}";
        FifoCaption = MovementItems.Count == 0
            ? "Для этого товара пока нет принятых партий. Как только появится поставка или возврат, они отобразятся здесь в очереди FIFO."
            : "Продажи распределяются по FIFO: сначала списываются самые ранние принятые партии, затем более поздние. Возвраты непродовольственных товаров попадают в конец этой очереди по дате возврата.";
        ReturnPolicyCaption = CanCreateReturn
            ? "Для категорий, отличных от \"Чай\", можно оформить возврат как новое входящее движение."
            : "Для категории \"Чай\" возврат на склад отключён.";
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

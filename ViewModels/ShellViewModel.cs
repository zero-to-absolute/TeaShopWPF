using System.Collections.ObjectModel;
using System.Windows.Input;
using TeaShopManager.DataAccess;
using TeaShopManager.Models;

namespace TeaShopManager.ViewModels;

public sealed class ShellViewModel : ViewModelBase
{
    private readonly TeaShopRepository _repository;
    private readonly RelayCommand _signOutCommand;
    private NavigationItem? _selectedNavigationItem;
    private string _pageTitle = string.Empty;
    private string _currentSectionKey = string.Empty;
    private string _productSearchText = string.Empty;
    private bool _isShowingCriticalProductsOnly;
    private string _currentOrderTitle = "Текущий заказ";
    private string _currentOrderDate = DateTime.Now.ToString("dd MMMM, HH:mm");
    private string _currentOrderTotal = "0 ₽";
    private List<ProductPreviewItem> _allProducts = [];

    public ShellViewModel(UserSession currentUser, TeaShopRepository repository)
    {
        CurrentUser = currentUser;
        _repository = repository;
        _signOutCommand = new RelayCommand(_ => SignOutRequested?.Invoke());

        NavigationItems = new ObservableCollection<NavigationItem>(BuildNavigation(currentUser.RoleName));
        Users = [];
        Categories = [];
        Roles = [];
        Products = [];
        Suppliers = [];
        StatusOrders = [];
        StatusSupplies = [];
        Orders = [];
        OrderDetails = [];
        Notifications = [];
        Supplies = [];
        SupplyDetails = [];
        Losses = [];
        AccountantReports = new ObservableCollection<ReportOption>(BuildAccountantReports());
        AdminReportCards = new ObservableCollection<AdminReportCard>(BuildAdminReportCards());
        SaleLines = [];
        QuickSaleItems = [];

        SelectedNavigationItem = NavigationItems.FirstOrDefault();
        ResetSaleDraft();
    }

    public event Action? SignOutRequested;

    public UserSession CurrentUser { get; }
    public ObservableCollection<NavigationItem> NavigationItems { get; }
    public ObservableCollection<UserPreviewItem> Users { get; }
    public ObservableCollection<CategoryPreviewItem> Categories { get; }
    public ObservableCollection<RolePreviewItem> Roles { get; }
    public ObservableCollection<ProductPreviewItem> Products { get; }
    public ObservableCollection<SupplierPreviewItem> Suppliers { get; }
    public ObservableCollection<StatusPreviewItem> StatusOrders { get; }
    public ObservableCollection<StatusPreviewItem> StatusSupplies { get; }
    public ObservableCollection<OrderPreviewItem> Orders { get; }
    public ObservableCollection<OrderDetailPreviewItem> OrderDetails { get; }
    public ObservableCollection<NotificationPreviewItem> Notifications { get; }
    public ObservableCollection<SupplyPreviewItem> Supplies { get; }
    public ObservableCollection<SupplyDetailPreviewItem> SupplyDetails { get; }
    public ObservableCollection<LossPreviewItem> Losses { get; }
    public ObservableCollection<ReportOption> AccountantReports { get; }
    public ObservableCollection<AdminReportCard> AdminReportCards { get; }
    public ObservableCollection<SaleLinePreviewItem> SaleLines { get; }
    public ObservableCollection<QuickSaleItem> QuickSaleItems { get; }

    public NavigationItem? SelectedNavigationItem
    {
        get => _selectedNavigationItem;
        set
        {
            if (!SetProperty(ref _selectedNavigationItem, value) || value is null) return;
            CurrentSectionKey = value.SectionKey;
            PageTitle = value.Title;
        }
    }

    public string CurrentSectionKey { get => _currentSectionKey; private set => SetProperty(ref _currentSectionKey, value); }
    public string PageTitle { get => _pageTitle; private set => SetProperty(ref _pageTitle, value); }
    public string WelcomeTitle => CurrentUser.DisplayName;
    public string RoleCaption => $"Роль: {CurrentUser.RoleName}";
    public bool IsAdminRole => RoleTag == "ADMIN";
    public bool IsAccountantRole => RoleTag == "ACCOUNTANT";
    public bool IsManagerRole => RoleTag == "MANAGER";
    public bool IsSellerRole => RoleTag == "SALES";
    public ICommand SignOutCommand => _signOutCommand;

    public string ProductSearchText
    {
        get => _productSearchText;
        set
        {
            if (!SetProperty(ref _productSearchText, value)) return;
            ApplyProductFilter();
        }
    }

    public bool IsShowingCriticalProductsOnly
    {
        get => _isShowingCriticalProductsOnly;
        private set
        {
            if (!SetProperty(ref _isShowingCriticalProductsOnly, value))
            {
                return;
            }

            OnPropertyChanged(nameof(IsShowingAllProducts));
            ApplyProductFilter();
        }
    }

    public bool IsShowingAllProducts => !IsShowingCriticalProductsOnly;

    public string ProductInventoryCaption => IsShowingCriticalProductsOnly
        ? $"Показаны только товары ниже минимального остатка: {Products.Count} поз. Двойной щелчок открывает партии и FIFO-остатки."
        : "Показаны все товары. Красным отмечены позиции с критическим остатком. Двойной щелчок открывает партии и FIFO-остатки.";

    public string CurrentOrderTitle { get => _currentOrderTitle; private set => SetProperty(ref _currentOrderTitle, value); }
    public string CurrentOrderDate { get => _currentOrderDate; private set => SetProperty(ref _currentOrderDate, value); }
    public string CurrentOrderTotal { get => _currentOrderTotal; private set => SetProperty(ref _currentOrderTotal, value); }

    public string RoleTag
    {
        get
        {
            var role = CurrentUser.RoleName.ToLowerInvariant();
            if (role.Contains("менедж")) return "MANAGER";
            if (role.Contains("продав")) return "SALES";
            if (role.Contains("бухгалтер")) return "ACCOUNTANT";
            return "ADMIN";
        }
    }

    public void LoadData()
    {
        ReplaceCollection(Roles, _repository.GetRoles());
        ReplaceCollection(Categories, _repository.GetCategories());
        ReplaceCollection(Users, _repository.GetUsers());
        _allProducts = _repository.GetProducts().ToList();
        ApplyProductFilter();
        ReplaceCollection(Suppliers, _repository.GetSuppliers());
        ReplaceCollection(StatusOrders, _repository.GetStatusOrders());
        ReplaceCollection(StatusSupplies, _repository.GetStatusSupplies());
        ReplaceCollection(Orders, _repository.GetOrders());
        ReplaceCollection(OrderDetails, _repository.GetOrderDetails());
        ReplaceCollection(Notifications, _repository.GetNotifications());
        ReplaceCollection(Supplies, _repository.GetSupplies());
        ReplaceCollection(SupplyDetails, _repository.GetSupplyDetails());
        ReplaceCollection(Losses, _repository.GetLosses());
        RebuildQuickSaleItems();
        UpdateOrderTotal();
    }

    public IReadOnlyList<LookupItem> GetRoleLookupItems() => _repository.GetRoleLookupItems();
    public IReadOnlyList<LookupItem> GetCategoryLookupItems() => _repository.GetCategoryLookupItems();
    public IReadOnlyList<LookupItem> GetUserLookupItems() => _repository.GetUserLookupItems();
    public IReadOnlyList<LookupItem> GetProductLookupItems() => _repository.GetProductLookupItems();
    public IReadOnlyList<LookupItem> GetSupplierLookupItems() => _repository.GetSupplierLookupItems();
    public IReadOnlyList<LookupItem> GetOrderLookupItems() => _repository.GetOrderLookupItems();
    public IReadOnlyList<LookupItem> GetSupplyLookupItems() => _repository.GetSupplyLookupItems();
    public IReadOnlyList<LookupItem> GetStatusOrderLookupItems() => _repository.GetStatusOrderLookupItems();
    public IReadOnlyList<LookupItem> GetStatusSupplyLookupItems() => _repository.GetStatusSupplyLookupItems();
    public ProductPreviewItem? GetProductById(int productId) => _allProducts.FirstOrDefault(product => product.ProductId == productId);
    public IReadOnlyList<ProductMovementPreviewItem> GetProductMovements(int productId) => _repository.GetProductMovements(productId);
    public CustomerReturnEditorModel GetCustomerReturnDraft(int productId) => _repository.GetCustomerReturnDraft(productId);
    public IReadOnlyList<ProductPreviewItem> GetProductsForSale() => _allProducts
        .Where(product => product.StockQuantityValue > 0)
        .OrderBy(product => product.ProductName)
        .ToList();

    public void SaveUser(UserEditorModel model) { _repository.SaveUser(model); ReplaceCollection(Users, _repository.GetUsers()); }
    public void DeleteUser(int userId) { _repository.DeleteUser(userId); ReplaceCollection(Users, _repository.GetUsers()); }
    public void SaveCategory(CategoryEditorModel model) { _repository.SaveCategory(model); ReplaceCollection(Categories, _repository.GetCategories()); }
    public void DeleteCategory(short categoryId) { _repository.DeleteCategory(categoryId); ReplaceCollection(Categories, _repository.GetCategories()); }
    public void SaveRole(RoleEditorModel model) { _repository.SaveRole(model); ReplaceCollection(Roles, _repository.GetRoles()); }
    public void DeleteRole(short roleId) { _repository.DeleteRole(roleId); ReplaceCollection(Roles, _repository.GetRoles()); }
    public void SaveSupplier(SupplierEditorModel model) { _repository.SaveSupplier(model); ReplaceCollection(Suppliers, _repository.GetSuppliers()); }
    public void DeleteSupplier(int supplierId) { _repository.DeleteSupplier(supplierId); ReplaceCollection(Suppliers, _repository.GetSuppliers()); }
    public void SaveStatusOrder(StatusEditorModel model) { _repository.SaveStatusOrder(model); ReplaceCollection(StatusOrders, _repository.GetStatusOrders()); }
    public void DeleteStatusOrder(int statusId) { _repository.DeleteStatusOrder(statusId); ReplaceCollection(StatusOrders, _repository.GetStatusOrders()); }
    public void SaveStatusSupply(StatusEditorModel model) { _repository.SaveStatusSupply(model); ReplaceCollection(StatusSupplies, _repository.GetStatusSupplies()); }
    public void DeleteStatusSupply(int statusId) { _repository.DeleteStatusSupply(statusId); ReplaceCollection(StatusSupplies, _repository.GetStatusSupplies()); }
    public void SaveOrder(OrderEditorModel model) { _repository.SaveOrder(model); ReplaceCollection(Orders, _repository.GetOrders()); }
    public void DeleteOrder(int orderId) { _repository.DeleteOrder(orderId); ReplaceCollection(Orders, _repository.GetOrders()); ReplaceCollection(OrderDetails, _repository.GetOrderDetails()); }
    public void SaveOrderDetail(OrderDetailEditorModel model) { _repository.SaveOrderDetail(model); ReplaceCollection(OrderDetails, _repository.GetOrderDetails()); ReplaceCollection(Orders, _repository.GetOrders()); }
    public void DeleteOrderDetail(int orderId, int productId) { _repository.DeleteOrderDetail(orderId, productId); ReplaceCollection(OrderDetails, _repository.GetOrderDetails()); ReplaceCollection(Orders, _repository.GetOrders()); }
    public void SaveNotification(NotificationEditorModel model) { _repository.SaveNotification(model); RefreshNotificationData(); }
    public void DeleteNotification(int notificationId) { _repository.DeleteNotification(notificationId); RefreshNotificationData(); }
    public void MarkNotificationAsRead(int notificationId) { _repository.MarkNotificationAsRead(notificationId); RefreshNotificationData(); }
    public void SaveSupply(SupplyEditorModel model) { _repository.SaveSupply(model); RefreshSupplyData(); RefreshInventoryData(); RefreshNotificationData(); }
    public void DeleteSupply(int supplyId) { _repository.DeleteSupply(supplyId); RefreshSupplyData(); }
    public void ReceiveSupply(int supplyId) { _repository.ReceiveSupply(supplyId); RefreshSupplyData(); RefreshInventoryData(); RefreshNotificationData(); }
    public void SaveSupplyDetail(SupplyDetailEditorModel model) { _repository.SaveSupplyDetail(model); RefreshSupplyData(); }
    public void DeleteSupplyDetail(int supplyId, int productId) { _repository.DeleteSupplyDetail(supplyId, productId); RefreshSupplyData(); }
    public void SaveLoss(LossEditorModel model) { _repository.SaveLoss(model); ReplaceCollection(Losses, _repository.GetLosses()); }
    public void DeleteLoss(int lossId) { _repository.DeleteLoss(lossId); ReplaceCollection(Losses, _repository.GetLosses()); }

    public void SaveProduct(ProductEditorModel model)
    {
        _repository.SaveProduct(model);
        RefreshInventoryData();
        RefreshNotificationData();
    }

    public void DeleteProduct(int productId)
    {
        _repository.DeleteProduct(productId);
        RefreshInventoryData();
        RefreshNotificationData();
    }

    public void CreateCustomerReturn(CustomerReturnEditorModel model)
    {
        _repository.CreateCustomerReturn(model, CurrentUser.UserId);
        RefreshSupplyData();
        RefreshInventoryData();
        RefreshNotificationData();
    }

    public void AddSaleLine(ProductPreviewItem product, int quantity)
    {
        if (quantity <= 0)
        {
            throw new InvalidOperationException("Количество должно быть больше нуля.");
        }

        var existing = SaleLines.FirstOrDefault(line => line.ProductId == product.ProductId);
        var requestedQuantity = quantity + (existing?.QuantityValue ?? 0);
        if (requestedQuantity > product.StockQuantityValue)
        {
            var details = product.StockQuantityValue > 0
                ? $"Доступно: {product.StockQuantityValue}."
                : "Товара сейчас нет в наличии.";

            throw new InvalidOperationException($"Недостаточно товара \"{product.ProductName}\". {details}");
        }

        if (existing is not null)
        {
            SaleLines.Remove(existing);
        }

        SaleLines.Add(new SaleLinePreviewItem
        {
            ProductId = product.ProductId,
            ProductName = product.ProductName,
            QuantityValue = requestedQuantity,
            Quantity = requestedQuantity.ToString(),
            UnitPriceValue = product.PriceValue,
            Price = $"{product.PriceValue:0.##} ₽",
            TotalAmount = $"{product.PriceValue * requestedQuantity:0.##} ₽"
        });

        UpdateOrderTotal();
    }

    public void RemoveSaleLine(int productId)
    {
        var line = SaleLines.FirstOrDefault(item => item.ProductId == productId);
        if (line is null) return;
        SaleLines.Remove(line);
        UpdateOrderTotal();
    }

    public void CancelCurrentSale() => ResetSaleDraft();

    public int CompleteCurrentSale()
    {
        var orderId = _repository.CreateSale(CurrentUser.UserId, SaleLines.Select(line => new SaleDraftLine
        {
            ProductId = line.ProductId,
            ProductName = line.ProductName,
            Quantity = line.QuantityValue,
            UnitPrice = line.UnitPriceValue
        }).ToList());

        ReplaceCollection(Orders, _repository.GetOrders());
        ReplaceCollection(OrderDetails, _repository.GetOrderDetails());
        RefreshInventoryData();
        RefreshNotificationData();
        ResetSaleDraft(orderId);
        return orderId;
    }

    public void ShowAllProducts() => IsShowingCriticalProductsOnly = false;

    public void ShowCriticalProducts() => IsShowingCriticalProductsOnly = true;

    private void ApplyProductFilter()
    {
        IEnumerable<ProductPreviewItem> filtered = IsShowingCriticalProductsOnly
            ? _allProducts.Where(product => product.IsCritical)
            : _allProducts;

        if (!string.IsNullOrWhiteSpace(ProductSearchText))
        {
            var search = ProductSearchText.Trim();
            filtered = filtered.Where(product =>
                product.ProductName.Contains(search, StringComparison.CurrentCultureIgnoreCase)
                || product.CategoryName.Contains(search, StringComparison.CurrentCultureIgnoreCase));
        }

        ReplaceCollection(Products, filtered.OrderBy(product => product.ProductName));
        OnPropertyChanged(nameof(ProductInventoryCaption));
    }

    private void RefreshInventoryData()
    {
        _allProducts = _repository.GetProducts().ToList();
        ApplyProductFilter();
        RebuildQuickSaleItems();
    }

    private void RefreshSupplyData()
    {
        ReplaceCollection(Supplies, _repository.GetSupplies());
        ReplaceCollection(SupplyDetails, _repository.GetSupplyDetails());
    }

    private void RefreshNotificationData()
    {
        ReplaceCollection(Notifications, _repository.GetNotifications());
    }

    private void RebuildQuickSaleItems() => ReplaceCollection(QuickSaleItems, _allProducts
        .Where(product => product.StockQuantityValue > 0)
        .OrderBy(product => product.ProductName)
        .Take(4)
        .Select(product => new QuickSaleItem
    {
        ProductId = product.ProductId,
        Title = product.ProductName,
        Price = product.Price
    }));

    private void ResetSaleDraft(int? lastOrderId = null)
    {
        SaleLines.Clear();
        CurrentOrderTitle = lastOrderId.HasValue ? $"Новый заказ после #{lastOrderId}" : "Текущий заказ";
        CurrentOrderDate = DateTime.Now.ToString("dd MMMM, HH:mm");
        UpdateOrderTotal();
    }

    private void UpdateOrderTotal() => CurrentOrderTotal = $"{SaleLines.Sum(line => line.UnitPriceValue * line.QuantityValue):0.##} ₽";

    private static void ReplaceCollection<T>(ObservableCollection<T> target, IEnumerable<T> items)
    {
        target.Clear();
        foreach (var item in items) target.Add(item);
    }

    private static IReadOnlyList<NavigationItem> BuildNavigation(string roleName)
    {
        var role = roleName.ToLowerInvariant();

        if (role.Contains("менедж"))
        {
            return
            [
                new NavigationItem { SectionKey = "Products", Title = "Управление товарами", IconGlyph = "\uE14C" },
                new NavigationItem { SectionKey = "Categories", Title = "Категории", IconGlyph = "\uE8FD" },
                new NavigationItem { SectionKey = "Suppliers", Title = "Управление поставщиками", IconGlyph = "\uE7BF" },
                new NavigationItem { SectionKey = "Orders", Title = "Заказы", IconGlyph = "\uE8A1" },
                new NavigationItem { SectionKey = "Notifications", Title = "Уведомления", IconGlyph = "\uE7F4", HasAttentionMark = true }
            ];
        }

        if (role.Contains("продав"))
        {
            return
            [
                new NavigationItem { SectionKey = "Sales", Title = "Продажа", IconGlyph = "\uEAFD" },
                new NavigationItem { SectionKey = "Products", Title = "Товары", IconGlyph = "\uE14C" }
            ];
        }

        if (role.Contains("бухгалтер"))
        {
            return [new NavigationItem { SectionKey = "Reports", Title = "Отчёты", IconGlyph = "\uE9D2" }];
        }

        return
        [
            new NavigationItem { SectionKey = "Users", Title = "Управление пользователями", IconGlyph = "\uE716" },
            new NavigationItem { SectionKey = "Roles", Title = "Роли", IconGlyph = "\uE902" },
            new NavigationItem { SectionKey = "Categories", Title = "Категории", IconGlyph = "\uE8FD" },
            new NavigationItem { SectionKey = "Products", Title = "Управление товарами", IconGlyph = "\uE14C" },
            new NavigationItem { SectionKey = "Suppliers", Title = "Управление поставщиками", IconGlyph = "\uE7BF" },
            new NavigationItem { SectionKey = "StatusOrders", Title = "Статусы заказов", IconGlyph = "\uE8B8" },
            new NavigationItem { SectionKey = "StatusSupplies", Title = "Статусы поставок", IconGlyph = "\uE8B8" },
            new NavigationItem { SectionKey = "Orders", Title = "Заказы", IconGlyph = "\uE8A1" },
            new NavigationItem { SectionKey = "OrderDetails", Title = "Состав заказов", IconGlyph = "\uE8A1" },
            new NavigationItem { SectionKey = "Notifications", Title = "Уведомления", IconGlyph = "\uE7F4" },
            new NavigationItem { SectionKey = "Supplies", Title = "Поставки", IconGlyph = "\uE7BF" },
            new NavigationItem { SectionKey = "SupplyDetails", Title = "Состав поставок", IconGlyph = "\uE8D2" },
            new NavigationItem { SectionKey = "Losses", Title = "Списания", IconGlyph = "\uE74D" },
            new NavigationItem { SectionKey = "Reports", Title = "Отчёты", IconGlyph = "\uE9D2" }
        ];
    }

    private static IReadOnlyList<ReportOption> BuildAccountantReports() =>
    [
        new ReportOption { Number = 1, Title = "Продажи по контрагентам" },
        new ReportOption { Number = 2, Title = "Продажи по товарным группам" },
        new ReportOption { Number = 3, Title = "Кассовый отчёт" }
    ];

    private static IReadOnlyList<AdminReportCard> BuildAdminReportCards() =>
    [
        new AdminReportCard { Title = "По продажам", Description = "Аналитика общих продаж за выбранный период времени.", AccentColor = "#E7C8B7" },
        new AdminReportCard { Title = "По прибыльности", Description = "Оценка маржинальности товаров и чистой прибыли магазина.", AccentColor = "#CDEBDD" },
        new AdminReportCard { Title = "По убыткам", Description = "Анализ списаний, просроченной продукции и возвратов.", AccentColor = "#F2D8D8" }
    ];
}

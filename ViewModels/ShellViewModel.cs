using System.Collections.ObjectModel;
using System.Text;
using System.Windows.Input;
using TeaShopManager.DataAccess;
using TeaShopManager.Models;

namespace TeaShopManager.ViewModels;

public sealed class ShellViewModel : ViewModelBase
{
    private readonly TeaShopRepository _repository;
    private readonly RelayCommand _signOutCommand;
    private readonly UserRoleAccess _roleAccess;
    private NavigationItem? _selectedNavigationItem;
    private string _pageTitle = string.Empty;
    private string _currentSectionKey = string.Empty;
    private string _productSearchText = string.Empty;
    private bool _isShowingCriticalProductsOnly;
    private string _currentOrderTitle = "Текущий заказ";
    private string _currentOrderDate = DateTime.Now.ToString("dd MMMM, HH:mm");
    private string _currentOrderTotal = "0 ₽";
    private ReportOption? _selectedAccountantReport;
    private LookupItem? _selectedReportSeller;
    private LookupItem? _selectedReportCategory;
    private DateTime _accountantReportStartDate = new(DateTime.Today.Year, DateTime.Today.Month, 1);
    private DateTime _accountantReportEndDate = DateTime.Today;
    private string _accountantReportStatusText = "Выберите параметры и нажмите «Сформировать».";
    private string _accountantReportFilterSummary = "Фильтры не применялись.";
    private string _accountantReportGeneratedAt = "Отчёт ещё не сформирован.";
    private string _expirationWarningDaysText = "14";
    private string _settingsStatusText = "Измените параметры и сохраните.";
    private string _databaseServerName = string.Empty;
    private string _databaseName = string.Empty;
    private string _databaseAuthenticationMode = string.Empty;
    private int _unreadNotificationsCount;
    private List<ProductPreviewItem> _allProducts = [];

    public ShellViewModel(UserSession currentUser, TeaShopRepository repository)
    {
        CurrentUser = currentUser;
        _repository = repository;
        _roleAccess = UserRoleAccessResolver.Resolve(currentUser.RoleName);
        _signOutCommand = new RelayCommand(_ => SignOutRequested?.Invoke());

        NavigationItems = new ObservableCollection<NavigationItem>(BuildNavigation(_roleAccess));
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
        ReportSellerOptions = [];
        ReportCategoryOptions = [];
        SalesBySellerReportRows = [];
        SalesByCategoryReportRows = [];
        CashReportRows = [];
        AccountantReportMetricCards = [];
        AdminReportCards = new ObservableCollection<AdminReportCard>(BuildAdminReportCards());
        SaleLines = [];
        QuickSaleItems = [];
        SelectedAccountantReport = AccountantReports.FirstOrDefault();
        LoadApplicationSettings();

        SelectedNavigationItem = NavigationItems.FirstOrDefault();
        if (SelectedNavigationItem is null)
        {
            PageTitle = "Нет доступных разделов";
        }

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
    public ObservableCollection<LookupItem> ReportSellerOptions { get; }
    public ObservableCollection<LookupItem> ReportCategoryOptions { get; }
    public ObservableCollection<SalesBySellerReportRow> SalesBySellerReportRows { get; }
    public ObservableCollection<SalesByCategoryReportRow> SalesByCategoryReportRows { get; }
    public ObservableCollection<CashReportRow> CashReportRows { get; }
    public ObservableCollection<ReportMetricCard> AccountantReportMetricCards { get; }
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

            if (value.SectionKey == "Reports" && CanUseAccountantReports)
            {
                LoadAccountantReportLookups();
            }

            if (value.SectionKey == "Settings" && CanManageSettings)
            {
                LoadApplicationSettings();
            }
        }
    }

    public string CurrentSectionKey { get => _currentSectionKey; private set => SetProperty(ref _currentSectionKey, value); }
    public string PageTitle { get => _pageTitle; private set => SetProperty(ref _pageTitle, value); }
    public string WelcomeTitle => CurrentUser.DisplayName;
    public string RoleCaption => $"Роль: {CurrentUser.RoleName}";
    public bool IsAdminRole => _roleAccess.HasAccess(UserRoleAccess.Administrator);
    public bool IsDirectorRole => _roleAccess.HasAccess(UserRoleAccess.Director);
    public bool IsAccountantRole => _roleAccess.HasAccess(UserRoleAccess.Accountant);
    public bool IsManagerRole => _roleAccess.HasAccess(UserRoleAccess.Manager);
    public bool IsSellerRole => _roleAccess.HasAccess(UserRoleAccess.Seller);
    public bool CanUseAccountantReports => IsAdminRole || IsAccountantRole;
    public bool CanViewManagementReports => IsAdminRole || IsDirectorRole;
    public bool CanManageSettings => IsAdminRole;
    public ICommand SignOutCommand => _signOutCommand;
    public string ExpirationWarningDaysText
    {
        get => _expirationWarningDaysText;
        set => SetProperty(ref _expirationWarningDaysText, value);
    }

    public string SettingsStatusText
    {
        get => _settingsStatusText;
        private set => SetProperty(ref _settingsStatusText, value);
    }

    public string DatabaseServerName
    {
        get => _databaseServerName;
        private set => SetProperty(ref _databaseServerName, value);
    }

    public string DatabaseName
    {
        get => _databaseName;
        private set => SetProperty(ref _databaseName, value);
    }

    public string DatabaseAuthenticationMode
    {
        get => _databaseAuthenticationMode;
        private set => SetProperty(ref _databaseAuthenticationMode, value);
    }

    public int UnreadNotificationsCount
    {
        get => _unreadNotificationsCount;
        private set => SetProperty(ref _unreadNotificationsCount, value);
    }

    public bool HasUnreadNotifications => UnreadNotificationsCount > 0;
    public string NotificationsSummaryText => Notifications.Count == 0
        ? "Уведомлений нет."
        : $"Всего: {Notifications.Count}. Непрочитано: {UnreadNotificationsCount}.";

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
        ? $"Критические остатки: {Products.Count} поз. Двойной щелчок показывает партии FIFO."
        : "Все товары. Двойной щелчок показывает партии FIFO.";

    public string CurrentOrderTitle { get => _currentOrderTitle; private set => SetProperty(ref _currentOrderTitle, value); }
    public string CurrentOrderDate { get => _currentOrderDate; private set => SetProperty(ref _currentOrderDate, value); }
    public string CurrentOrderTotal { get => _currentOrderTotal; private set => SetProperty(ref _currentOrderTotal, value); }

    public ReportOption? SelectedAccountantReport
    {
        get => _selectedAccountantReport;
        set
        {
            if (!SetProperty(ref _selectedAccountantReport, value)) return;
            OnPropertyChanged(nameof(SelectedAccountantReportDescription));
            OnPropertyChanged(nameof(ShowSellerReportFilter));
            OnPropertyChanged(nameof(ShowCategoryReportFilter));
            OnPropertyChanged(nameof(IsSalesBySellerReportSelected));
            OnPropertyChanged(nameof(IsSalesByCategoryReportSelected));
            OnPropertyChanged(nameof(IsCashReportSelected));
            OnPropertyChanged(nameof(AccountantReportPreviewTitle));
            OnPropertyChanged(nameof(HasAccountantReportData));
            ResetAccountantReportPreview($"Выберите параметры для отчёта «{value?.Title ?? "Бухгалтерский отчёт"}» и нажмите «Сформировать».");
        }
    }

    public LookupItem? SelectedReportSeller
    {
        get => _selectedReportSeller;
        set => SetProperty(ref _selectedReportSeller, value);
    }

    public LookupItem? SelectedReportCategory
    {
        get => _selectedReportCategory;
        set => SetProperty(ref _selectedReportCategory, value);
    }

    public DateTime AccountantReportStartDate
    {
        get => _accountantReportStartDate;
        set => SetProperty(ref _accountantReportStartDate, value.Date);
    }

    public DateTime AccountantReportEndDate
    {
        get => _accountantReportEndDate;
        set => SetProperty(ref _accountantReportEndDate, value.Date);
    }

    public string SelectedAccountantReportDescription => SelectedAccountantReport?.Description ?? string.Empty;
    public string AccountantReportPreviewTitle => SelectedAccountantReport is null ? "Предпросмотр" : $"Предпросмотр: {SelectedAccountantReport.Title}";
    public string AccountantReportStatusText { get => _accountantReportStatusText; private set => SetProperty(ref _accountantReportStatusText, value); }
    public string AccountantReportFilterSummary { get => _accountantReportFilterSummary; private set => SetProperty(ref _accountantReportFilterSummary, value); }
    public string AccountantReportGeneratedAt { get => _accountantReportGeneratedAt; private set => SetProperty(ref _accountantReportGeneratedAt, value); }
    public bool ShowSellerReportFilter => SelectedAccountantReport?.Key is AccountantReportKeys.SalesBySeller or AccountantReportKeys.Cash;
    public bool ShowCategoryReportFilter => SelectedAccountantReport?.Key == AccountantReportKeys.SalesByCategory;
    public bool IsSalesBySellerReportSelected => SelectedAccountantReport?.Key == AccountantReportKeys.SalesBySeller;
    public bool IsSalesByCategoryReportSelected => SelectedAccountantReport?.Key == AccountantReportKeys.SalesByCategory;
    public bool IsCashReportSelected => SelectedAccountantReport?.Key == AccountantReportKeys.Cash;
    public bool HasAccountantReportData => IsSalesBySellerReportSelected && SalesBySellerReportRows.Count > 0
        || IsSalesByCategoryReportSelected && SalesByCategoryReportRows.Count > 0
        || IsCashReportSelected && CashReportRows.Count > 0;

    public string RoleTag
    {
        get
        {
            var tags = new List<string>();
            if (IsAdminRole) tags.Add("ADMIN");
            if (IsDirectorRole) tags.Add("DIRECTOR");
            if (IsManagerRole) tags.Add("MANAGER");
            if (IsSellerRole) tags.Add("SALES");
            if (IsAccountantRole) tags.Add("ACCOUNTANT");
            return tags.Count == 0 ? "NO ACCESS" : string.Join(" / ", tags);
        }
    }

    public void LoadData()
    {
        LoadApplicationSettings();
        ReplaceCollection(Roles, CanManageUsers || CanManageStatuses ? _repository.GetRoles() : Array.Empty<RolePreviewItem>());
        ReplaceCollection(Categories, CanManageCatalog ? _repository.GetCategories() : Array.Empty<CategoryPreviewItem>());
        ReplaceCollection(Users, CanManageUsers ? _repository.GetUsers() : Array.Empty<UserPreviewItem>());

        if (CanViewProducts)
        {
            _allProducts = _repository.GetProducts().ToList();
            ApplyProductFilter();
            RebuildQuickSaleItems();
        }
        else
        {
            _allProducts = [];
            ApplyProductFilter();
            QuickSaleItems.Clear();
        }

        ReplaceCollection(Suppliers, CanManageSuppliers ? _repository.GetSuppliers() : Array.Empty<SupplierPreviewItem>());
        ReplaceCollection(StatusOrders, CanManageStatuses || CanManageOrders ? _repository.GetStatusOrders() : Array.Empty<StatusPreviewItem>());
        ReplaceCollection(StatusSupplies, CanManageStatuses || CanManageSupplies ? _repository.GetStatusSupplies() : Array.Empty<StatusPreviewItem>());
        ReplaceCollection(Orders, CanManageOrders ? _repository.GetOrders() : Array.Empty<OrderPreviewItem>());
        ReplaceCollection(OrderDetails, CanManageOrders ? _repository.GetOrderDetails() : Array.Empty<OrderDetailPreviewItem>());

        if (CanManageNotifications)
        {
            _repository.RefreshSystemNotifications();
        }

        ReplaceCollection(Notifications, CanManageNotifications ? _repository.GetNotifications() : Array.Empty<NotificationPreviewItem>());
        ReplaceCollection(Supplies, CanManageSupplies ? _repository.GetSupplies() : Array.Empty<SupplyPreviewItem>());
        ReplaceCollection(SupplyDetails, CanManageSupplies ? _repository.GetSupplyDetails() : Array.Empty<SupplyDetailPreviewItem>());
        ReplaceCollection(Losses, CanManageLosses ? _repository.GetLosses() : Array.Empty<LossPreviewItem>());
        UpdateNotificationState();

        if (CanUseAccountantReports)
        {
            LoadAccountantReportLookups();
            GenerateAccountantReport();
        }
        else
        {
            ReportSellerOptions.Clear();
            ReportCategoryOptions.Clear();
            SelectedReportSeller = null;
            SelectedReportCategory = null;
            ResetAccountantReportPreview("У текущей роли нет доступа к бухгалтерским отчётам.");
        }

        UpdateOrderTotal();
    }

    public IReadOnlyList<LookupItem> GetRoleLookupItems()
    {
        EnsurePermission(CanManageUsers, "Только администратор может выбирать роли пользователей.");
        return _repository.GetRoleLookupItems();
    }

    public IReadOnlyList<LookupItem> GetCategoryLookupItems()
    {
        EnsurePermission(CanManageCatalog, "Недостаточно прав для работы с категориями.");
        return _repository.GetCategoryLookupItems();
    }

    public IReadOnlyList<LookupItem> GetUserLookupItems()
    {
        EnsurePermission(CanManageUsers || CanManageOrders || CanManageSupplies || CanManageNotifications, "Недостаточно прав для выбора пользователя.");
        return _repository.GetUserLookupItems();
    }

    public IReadOnlyList<LookupItem> GetProductLookupItems()
    {
        EnsurePermission(CanViewProducts, "Недостаточно прав для выбора товара.");
        return _repository.GetProductLookupItems();
    }

    public IReadOnlyList<LookupItem> GetSupplierLookupItems()
    {
        EnsurePermission(CanManageSuppliers || CanManageSupplies, "Недостаточно прав для выбора поставщика.");
        return _repository.GetSupplierLookupItems();
    }

    public IReadOnlyList<LookupItem> GetOrderLookupItems()
    {
        EnsurePermission(CanManageOrders, "Недостаточно прав для выбора заказа.");
        return _repository.GetOrderLookupItems();
    }

    public IReadOnlyList<LookupItem> GetSupplyLookupItems()
    {
        EnsurePermission(CanManageSupplies, "Недостаточно прав для выбора поставки.");
        return _repository.GetSupplyLookupItems();
    }

    public IReadOnlyList<LookupItem> GetStatusOrderLookupItems()
    {
        EnsurePermission(CanManageOrders || CanManageStatuses, "Недостаточно прав для выбора статуса заказа.");
        return _repository.GetStatusOrderLookupItems();
    }

    public IReadOnlyList<LookupItem> GetStatusSupplyLookupItems()
    {
        EnsurePermission(CanManageSupplies || CanManageStatuses, "Недостаточно прав для выбора статуса поставки.");
        return _repository.GetStatusSupplyLookupItems();
    }

    public void GenerateAccountantReport()
    {
        EnsurePermission(CanUseAccountantReports, "Недостаточно прав для формирования бухгалтерских отчётов.");

        if (SelectedAccountantReport is null)
        {
            throw new InvalidOperationException("Выберите тип бухгалтерского отчёта.");
        }

        if (AccountantReportStartDate.Date > AccountantReportEndDate.Date)
        {
            throw new InvalidOperationException("Дата начала периода не может быть позже даты окончания.");
        }

        switch (SelectedAccountantReport.Key)
        {
            case AccountantReportKeys.SalesBySeller:
                GenerateSalesBySellerReport();
                break;
            case AccountantReportKeys.SalesByCategory:
                GenerateSalesByCategoryReport();
                break;
            case AccountantReportKeys.Cash:
                GenerateCashReport();
                break;
            default:
                throw new InvalidOperationException("Выбран неподдерживаемый тип отчёта.");
        }

        AccountantReportGeneratedAt = $"Сформирован: {DateTime.Now:dd.MM.yyyy HH:mm}";
        OnPropertyChanged(nameof(HasAccountantReportData));
    }

    public string GetAccountantReportFileName()
    {
        EnsurePermission(CanUseAccountantReports, "Недостаточно прав для выгрузки бухгалтерских отчётов.");

        var reportSlug = SelectedAccountantReport?.Key switch
        {
            AccountantReportKeys.SalesBySeller => "sales-by-sellers",
            AccountantReportKeys.SalesByCategory => "sales-by-categories",
            AccountantReportKeys.Cash => "cash-report",
            _ => "accountant-report"
        };

        return $"{reportSlug}_{AccountantReportStartDate:yyyyMMdd}_{AccountantReportEndDate:yyyyMMdd}.csv";
    }

    public string GetAccountantReportCsvContent()
    {
        EnsurePermission(CanUseAccountantReports, "Недостаточно прав для выгрузки бухгалтерских отчётов.");

        if (SelectedAccountantReport is null)
        {
            throw new InvalidOperationException("Сначала выберите тип отчёта.");
        }

        if (!HasAccountantReportData)
        {
            throw new InvalidOperationException("Сначала сформируйте отчёт и убедитесь, что в нём есть строки для выгрузки.");
        }

        var builder = new StringBuilder();
        AppendCsvRow(builder, "Отчёт", SelectedAccountantReport.Title);
        AppendCsvRow(builder, "Период", $"{AccountantReportStartDate:dd.MM.yyyy} - {AccountantReportEndDate:dd.MM.yyyy}");
        AppendCsvRow(builder, "Фильтр", GetSelectedFilterName());
        AppendCsvRow(builder, "Сформирован", DateTime.Now.ToString("dd.MM.yyyy HH:mm"));
        builder.AppendLine();

        switch (SelectedAccountantReport.Key)
        {
            case AccountantReportKeys.SalesBySeller:
                AppendCsvRow(builder, "Продавец", "Заказов", "Позиций", "Продано", "Выручка", "Средний чек");
                foreach (var row in SalesBySellerReportRows)
                {
                    AppendCsvRow(builder,
                        row.SellerName,
                        row.OrdersCountValue.ToString(),
                        row.PositionsCountValue.ToString(),
                        row.QuantityValue.ToString(),
                        FormatDecimal(row.RevenueValue),
                        FormatDecimal(row.AverageCheckValue));
                }

                break;
            case AccountantReportKeys.SalesByCategory:
                AppendCsvRow(builder, "Категория", "Заказов", "Товаров", "Продано", "Выручка", "Средняя цена");
                foreach (var row in SalesByCategoryReportRows)
                {
                    AppendCsvRow(builder,
                        row.CategoryName,
                        row.OrdersCountValue.ToString(),
                        row.ProductCountValue.ToString(),
                        row.QuantityValue.ToString(),
                        FormatDecimal(row.RevenueValue),
                        FormatDecimal(row.AveragePriceValue));
                }

                break;
            case AccountantReportKeys.Cash:
                AppendCsvRow(builder, "Дата", "Заказов", "Позиций", "Продано", "Выручка", "Средний чек");
                foreach (var row in CashReportRows)
                {
                    AppendCsvRow(builder,
                        row.ReportDateValue.ToString("dd.MM.yyyy"),
                        row.OrdersCountValue.ToString(),
                        row.PositionsCountValue.ToString(),
                        row.QuantityValue.ToString(),
                        FormatDecimal(row.RevenueValue),
                        FormatDecimal(row.AverageCheckValue));
                }

                break;
        }

        return builder.ToString();
    }

    public IReadOnlyList<CashReportRow> GetManagementSalesDailyReport(DateTime dateFrom, DateTime dateTo)
    {
        EnsurePermission(CanViewManagementReports, "Недостаточно прав для просмотра управленческих отчётов.");
        return _repository.GetCashReport(dateFrom, dateTo, null);
    }

    public IReadOnlyList<ManagementSalesTopProductRow> GetManagementSalesTopProducts(DateTime dateFrom, DateTime dateTo, int topCount = 10)
    {
        EnsurePermission(CanViewManagementReports, "Недостаточно прав для просмотра управленческих отчётов.");
        return _repository.GetManagementSalesTopProducts(dateFrom, dateTo, topCount);
    }

    public IReadOnlyList<ProfitabilityReportRow> GetManagementProfitabilityReport(DateTime dateFrom, DateTime dateTo, short? categoryId)
    {
        EnsurePermission(CanViewManagementReports, "Недостаточно прав для просмотра управленческих отчётов.");
        return _repository.GetProfitabilityReport(dateFrom, dateTo, categoryId);
    }

    public IReadOnlyList<LossAnalyticsReportRow> GetManagementLossAnalyticsReport(DateTime dateFrom, DateTime dateTo)
    {
        EnsurePermission(CanViewManagementReports, "Недостаточно прав для просмотра управленческих отчётов.");
        return _repository.GetLossAnalyticsReport(dateFrom, dateTo);
    }

    public IReadOnlyList<CustomerReturnAnalyticsRow> GetManagementCustomerReturnAnalyticsReport(DateTime dateFrom, DateTime dateTo)
    {
        EnsurePermission(CanViewManagementReports, "Недостаточно прав для просмотра управленческих отчётов.");
        return _repository.GetCustomerReturnAnalyticsReport(dateFrom, dateTo);
    }

    public IReadOnlyList<LookupItem> GetManagementCategoryLookupItems()
    {
        EnsurePermission(CanViewManagementReports, "Недостаточно прав для просмотра управленческих отчётов.");
        return _repository.GetReportCategoryLookupItems();
    }

    public ProductPreviewItem? GetProductById(int productId)
    {
        EnsurePermission(CanViewProducts, "Недостаточно прав для просмотра товаров.");
        return _allProducts.FirstOrDefault(product => product.ProductId == productId);
    }

    public IReadOnlyList<ProductMovementPreviewItem> GetProductMovements(int productId)
    {
        EnsurePermission(CanViewProducts, "Недостаточно прав для просмотра движения товара.");
        return _repository.GetProductMovements(productId);
    }

    public CustomerReturnEditorModel GetCustomerReturnDraft(int productId)
    {
        EnsurePermission(CanCreateCustomerReturns, "Недостаточно прав для оформления возврата.");
        return _repository.GetCustomerReturnDraft(productId);
    }

    public IReadOnlyList<ProductPreviewItem> GetProductsForSale()
    {
        EnsurePermission(CanPerformSales, "Недостаточно прав для оформления продажи.");
        return _allProducts
            .Where(product => product.StockQuantityValue > 0)
            .OrderBy(product => product.ProductName)
            .ToList();
    }

    public void SaveUser(UserEditorModel model)
    {
        EnsurePermission(CanManageUsers, "Только администратор может управлять пользователями.");
        _repository.SaveUser(model);
        ReplaceCollection(Users, _repository.GetUsers());
    }

    public void DeleteUser(int userId)
    {
        EnsurePermission(CanManageUsers, "Только администратор может удалять пользователей.");
        _repository.DeleteUser(userId);
        ReplaceCollection(Users, _repository.GetUsers());
    }

    public void SaveCategory(CategoryEditorModel model)
    {
        EnsurePermission(CanManageCatalog, "Недостаточно прав для изменения категорий.");
        _repository.SaveCategory(model);
        ReplaceCollection(Categories, _repository.GetCategories());
    }

    public void DeleteCategory(short categoryId)
    {
        EnsurePermission(CanManageCatalog, "Недостаточно прав для удаления категорий.");
        _repository.DeleteCategory(categoryId);
        ReplaceCollection(Categories, _repository.GetCategories());
    }

    public void SaveRole(RoleEditorModel model)
    {
        EnsurePermission(CanManageUsers, "Только администратор может изменять роли.");
        _repository.SaveRole(model);
        ReplaceCollection(Roles, _repository.GetRoles());
    }

    public void DeleteRole(short roleId)
    {
        EnsurePermission(CanManageUsers, "Только администратор может удалять роли.");
        _repository.DeleteRole(roleId);
        ReplaceCollection(Roles, _repository.GetRoles());
    }

    public void SaveSupplier(SupplierEditorModel model)
    {
        EnsurePermission(CanManageSuppliers, "Недостаточно прав для изменения поставщиков.");
        _repository.SaveSupplier(model);
        ReplaceCollection(Suppliers, _repository.GetSuppliers());
    }

    public void DeleteSupplier(int supplierId)
    {
        EnsurePermission(CanManageSuppliers, "Недостаточно прав для удаления поставщиков.");
        _repository.DeleteSupplier(supplierId);
        ReplaceCollection(Suppliers, _repository.GetSuppliers());
    }

    public void SaveStatusOrder(StatusEditorModel model)
    {
        EnsurePermission(CanManageStatuses, "Только администратор может изменять статусы.");
        _repository.SaveStatusOrder(model);
        ReplaceCollection(StatusOrders, _repository.GetStatusOrders());
    }

    public void DeleteStatusOrder(int statusId)
    {
        EnsurePermission(CanManageStatuses, "Только администратор может удалять статусы.");
        _repository.DeleteStatusOrder(statusId);
        ReplaceCollection(StatusOrders, _repository.GetStatusOrders());
    }

    public void SaveStatusSupply(StatusEditorModel model)
    {
        EnsurePermission(CanManageStatuses, "Только администратор может изменять статусы.");
        _repository.SaveStatusSupply(model);
        ReplaceCollection(StatusSupplies, _repository.GetStatusSupplies());
    }

    public void DeleteStatusSupply(int statusId)
    {
        EnsurePermission(CanManageStatuses, "Только администратор может удалять статусы.");
        _repository.DeleteStatusSupply(statusId);
        ReplaceCollection(StatusSupplies, _repository.GetStatusSupplies());
    }

    public void SaveOrder(OrderEditorModel model)
    {
        EnsurePermission(CanManageOrders, "Недостаточно прав для изменения заказов.");
        _repository.SaveOrder(model);
        ReplaceCollection(Orders, _repository.GetOrders());
        RefreshInventoryData();
        RefreshNotificationData();
    }

    public void DeleteOrder(int orderId)
    {
        EnsurePermission(CanManageOrders, "Недостаточно прав для удаления заказов.");
        _repository.DeleteOrder(orderId);
        ReplaceCollection(Orders, _repository.GetOrders());
        ReplaceCollection(OrderDetails, _repository.GetOrderDetails());
        RefreshInventoryData();
        RefreshNotificationData();
    }

    public void SaveOrderDetail(OrderDetailEditorModel model)
    {
        EnsurePermission(CanManageOrders, "Недостаточно прав для изменения состава заказов.");
        _repository.SaveOrderDetail(model);
        ReplaceCollection(OrderDetails, _repository.GetOrderDetails());
        ReplaceCollection(Orders, _repository.GetOrders());
        RefreshInventoryData();
        RefreshNotificationData();
    }

    public void DeleteOrderDetail(int orderId, int productId)
    {
        EnsurePermission(CanManageOrders, "Недостаточно прав для удаления позиций заказа.");
        _repository.DeleteOrderDetail(orderId, productId);
        ReplaceCollection(OrderDetails, _repository.GetOrderDetails());
        ReplaceCollection(Orders, _repository.GetOrders());
        RefreshInventoryData();
        RefreshNotificationData();
    }

    public void SaveNotification(NotificationEditorModel model)
    {
        EnsurePermission(CanManageNotifications, "Недостаточно прав для изменения уведомлений.");
        _repository.SaveNotification(model);
        RefreshNotificationData();
    }

    public void DeleteNotification(int notificationId)
    {
        EnsurePermission(CanManageNotifications, "Недостаточно прав для удаления уведомлений.");
        _repository.DeleteNotification(notificationId);
        RefreshNotificationData();
    }

    public void MarkNotificationAsRead(int notificationId)
    {
        EnsurePermission(CanManageNotifications, "Недостаточно прав для изменения уведомлений.");
        _repository.MarkNotificationAsRead(notificationId);
        RefreshNotificationData();
    }

    public void MarkAllNotificationsAsRead()
    {
        EnsurePermission(CanManageNotifications, "Недостаточно прав для изменения уведомлений.");
        _repository.MarkAllNotificationsAsRead();
        RefreshNotificationData();
    }

    public void SaveApplicationSettings()
    {
        EnsurePermission(CanManageSettings, "Только администратор может изменять настройки приложения.");

        if (!int.TryParse(ExpirationWarningDaysText, out var warningDays))
        {
            throw new InvalidOperationException("Порог уведомления по сроку годности должен быть целым числом.");
        }

        if (warningDays < 1 || warningDays > 90)
        {
            throw new InvalidOperationException("Порог уведомления по сроку годности должен быть в диапазоне от 1 до 90 дней.");
        }

        _repository.SaveApplicationSettings(new ApplicationSettingsModel
        {
            ExpirationWarningDays = warningDays
        });

        _repository.RefreshSystemNotifications();
        RefreshNotificationData();
        SettingsStatusText = $"Сохранено: порог {warningDays} дн. ({DateTime.Now:dd.MM.yyyy HH:mm}).";
    }

    public void SaveSupply(SupplyEditorModel model)
    {
        EnsurePermission(CanManageSupplies, "Недостаточно прав для изменения поставок.");
        _repository.SaveSupply(model);
        RefreshSupplyData();
        RefreshInventoryData();
        RefreshNotificationData();
    }

    public void DeleteSupply(int supplyId)
    {
        EnsurePermission(CanManageSupplies, "Недостаточно прав для удаления поставок.");
        _repository.DeleteSupply(supplyId);
        RefreshSupplyData();
    }

    public void ReceiveSupply(int supplyId)
    {
        EnsurePermission(CanManageSupplies, "Недостаточно прав для приёмки поставок.");
        _repository.ReceiveSupply(supplyId);
        RefreshSupplyData();
        RefreshInventoryData();
        RefreshNotificationData();
    }

    public void SaveSupplyDetail(SupplyDetailEditorModel model)
    {
        EnsurePermission(CanManageSupplies, "Недостаточно прав для изменения состава поставок.");
        _repository.SaveSupplyDetail(model);
        RefreshSupplyData();
    }

    public void DeleteSupplyDetail(int supplyId, int productId)
    {
        EnsurePermission(CanManageSupplies, "Недостаточно прав для удаления позиций поставки.");
        _repository.DeleteSupplyDetail(supplyId, productId);
        RefreshSupplyData();
    }

    public void SaveLoss(LossEditorModel model)
    {
        EnsurePermission(CanManageLosses, "Недостаточно прав для оформления списаний.");
        _repository.SaveLoss(model);
        ReplaceCollection(Losses, _repository.GetLosses());
        RefreshInventoryData();
        RefreshNotificationData();
    }

    public void DeleteLoss(int lossId)
    {
        EnsurePermission(CanManageLosses, "Недостаточно прав для удаления списаний.");
        _repository.DeleteLoss(lossId);
        ReplaceCollection(Losses, _repository.GetLosses());
        RefreshInventoryData();
        RefreshNotificationData();
    }

    public void SaveProduct(ProductEditorModel model)
    {
        EnsurePermission(CanManageCatalog, "Недостаточно прав для изменения товаров.");
        _repository.SaveProduct(model);
        RefreshInventoryData();
        RefreshNotificationData();
    }

    public void DeleteProduct(int productId)
    {
        EnsurePermission(CanManageCatalog, "Недостаточно прав для удаления товаров.");
        _repository.DeleteProduct(productId);
        RefreshInventoryData();
        RefreshNotificationData();
    }

    public void CreateCustomerReturn(CustomerReturnEditorModel model)
    {
        EnsurePermission(CanCreateCustomerReturns, "Недостаточно прав для оформления возврата.");
        _repository.CreateCustomerReturn(model, CurrentUser.UserId);
        RefreshSupplyData();
        RefreshInventoryData();
        RefreshNotificationData();
    }

    public void AddSaleLine(ProductPreviewItem product, int quantity)
    {
        EnsurePermission(CanPerformSales, "Недостаточно прав для оформления продажи.");
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
        EnsurePermission(CanPerformSales, "Недостаточно прав для изменения продажи.");
        var line = SaleLines.FirstOrDefault(item => item.ProductId == productId);
        if (line is null) return;
        SaleLines.Remove(line);
        UpdateOrderTotal();
    }

    public void CancelCurrentSale()
    {
        EnsurePermission(CanPerformSales, "Недостаточно прав для изменения продажи.");
        ResetSaleDraft();
    }

    public int CompleteCurrentSale()
    {
        EnsurePermission(CanPerformSales, "Недостаточно прав для сохранения продажи.");
        var orderId = _repository.CreateSale(CurrentUser.UserId, SaleLines.Select(line => new SaleDraftLine
        {
            ProductId = line.ProductId,
            ProductName = line.ProductName,
            Quantity = line.QuantityValue,
            UnitPrice = line.UnitPriceValue
        }).ToList());

        ReplaceCollection(Orders, CanManageOrders ? _repository.GetOrders() : Array.Empty<OrderPreviewItem>());
        ReplaceCollection(OrderDetails, CanManageOrders ? _repository.GetOrderDetails() : Array.Empty<OrderDetailPreviewItem>());
        RefreshInventoryData();
        RefreshNotificationData();
        ResetSaleDraft(orderId);
        return orderId;
    }

    public void ShowAllProducts()
    {
        EnsurePermission(CanViewProducts, "Недостаточно прав для просмотра товаров.");
        IsShowingCriticalProductsOnly = false;
    }

    public void ShowCriticalProducts()
    {
        EnsurePermission(CanViewProducts, "Недостаточно прав для просмотра товаров.");
        IsShowingCriticalProductsOnly = true;
    }

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
        if (!CanViewProducts)
        {
            _allProducts = [];
            ApplyProductFilter();
            QuickSaleItems.Clear();
            return;
        }

        _allProducts = _repository.GetProducts().ToList();
        ApplyProductFilter();
        RebuildQuickSaleItems();
    }

    private void RefreshSupplyData()
    {
        if (!CanManageSupplies)
        {
            Supplies.Clear();
            SupplyDetails.Clear();
            return;
        }

        ReplaceCollection(Supplies, _repository.GetSupplies());
        ReplaceCollection(SupplyDetails, _repository.GetSupplyDetails());
    }

    private void RefreshNotificationData()
    {
        if (!CanManageNotifications)
        {
            Notifications.Clear();
            UpdateNotificationState();
            return;
        }

        ReplaceCollection(Notifications, _repository.GetNotifications());
        UpdateNotificationState();
    }

    private void UpdateNotificationState()
    {
        UnreadNotificationsCount = Notifications.Count(item => !item.IsRead);
        OnPropertyChanged(nameof(HasUnreadNotifications));
        OnPropertyChanged(nameof(NotificationsSummaryText));
        UpdateNotificationAttentionMark();
    }

    private void UpdateNotificationAttentionMark()
    {
        var hasUnreadNotifications = UnreadNotificationsCount > 0;
        foreach (var item in NavigationItems.Where(item => item.SectionKey == "Notifications"))
        {
            item.HasAttentionMark = hasUnreadNotifications;
        }
    }

    private void LoadApplicationSettings()
    {
        var settings = _repository.GetApplicationSettings();
        var connectionInfo = _repository.GetDatabaseConnectionInfo();

        ExpirationWarningDaysText = settings.ExpirationWarningDays.ToString();
        DatabaseServerName = connectionInfo.ServerName;
        DatabaseName = connectionInfo.DatabaseName;
        DatabaseAuthenticationMode = connectionInfo.AuthenticationMode;

        if (string.IsNullOrWhiteSpace(SettingsStatusText))
        {
            SettingsStatusText = "Измените параметры и сохраните.";
        }
    }

    private void RebuildQuickSaleItems()
    {
        if (!CanPerformSales)
        {
            QuickSaleItems.Clear();
            return;
        }

        ReplaceCollection(QuickSaleItems, _allProducts
            .Where(product => product.StockQuantityValue > 0)
            .OrderBy(product => product.ProductName)
            .Take(4)
            .Select(product => new QuickSaleItem
            {
                ProductId = product.ProductId,
                Title = product.ProductName,
                Price = product.Price
            }));
    }

    private void ResetSaleDraft(int? lastOrderId = null)
    {
        SaleLines.Clear();
        CurrentOrderTitle = lastOrderId.HasValue ? $"Новый заказ после #{lastOrderId}" : "Текущий заказ";
        CurrentOrderDate = DateTime.Now.ToString("dd MMMM, HH:mm");
        UpdateOrderTotal();
    }

    private void UpdateOrderTotal() => CurrentOrderTotal = $"{SaleLines.Sum(line => line.UnitPriceValue * line.QuantityValue):0.##} ₽";

    private void LoadAccountantReportLookups()
    {
        var selectedSellerId = SelectedReportSeller?.Id;
        var selectedCategoryId = SelectedReportCategory?.Id;

        ReplaceCollection(ReportSellerOptions, BuildReportLookupItems("Все продавцы", _repository.GetReportSellerLookupItems()));
        ReplaceCollection(ReportCategoryOptions, BuildReportLookupItems("Все категории", _repository.GetReportCategoryLookupItems()));

        SelectedReportSeller = ReportSellerOptions.FirstOrDefault(item => item.Id == selectedSellerId)
            ?? ReportSellerOptions.FirstOrDefault();
        SelectedReportCategory = ReportCategoryOptions.FirstOrDefault(item => item.Id == selectedCategoryId)
            ?? ReportCategoryOptions.FirstOrDefault();
    }

    private void GenerateSalesBySellerReport()
    {
        var sellerId = SelectedReportSeller is { Id: > 0 } ? SelectedReportSeller.Id : (int?)null;
        var rows = _repository.GetSalesBySellerReport(AccountantReportStartDate, AccountantReportEndDate, sellerId);

        ReplaceCollection(SalesBySellerReportRows, rows);
        SalesByCategoryReportRows.Clear();
        CashReportRows.Clear();
        ReplaceCollection(AccountantReportMetricCards, BuildSalesBySellerMetricCards(rows));

        AccountantReportFilterSummary = BuildFilterSummary("Продавец", GetSelectedFilterName());
        AccountantReportStatusText = rows.Count == 0
            ? "По выбранным параметрам записи не найдены."
            : $"Строк в отчёте: {rows.Count}.";
    }

    private void GenerateSalesByCategoryReport()
    {
        var categoryId = SelectedReportCategory is { Id: > 0 }
            ? Convert.ToInt16(SelectedReportCategory.Id)
            : (short?)null;
        var rows = _repository.GetSalesByCategoryReport(AccountantReportStartDate, AccountantReportEndDate, categoryId);

        SalesBySellerReportRows.Clear();
        ReplaceCollection(SalesByCategoryReportRows, rows);
        CashReportRows.Clear();
        ReplaceCollection(AccountantReportMetricCards, BuildSalesByCategoryMetricCards(rows));

        AccountantReportFilterSummary = BuildFilterSummary("Категория", GetSelectedFilterName());
        AccountantReportStatusText = rows.Count == 0
            ? "По выбранным параметрам записи не найдены."
            : $"Строк в отчёте: {rows.Count}.";
    }

    private void GenerateCashReport()
    {
        var sellerId = SelectedReportSeller is { Id: > 0 } ? SelectedReportSeller.Id : (int?)null;
        var rows = _repository.GetCashReport(AccountantReportStartDate, AccountantReportEndDate, sellerId);

        SalesBySellerReportRows.Clear();
        SalesByCategoryReportRows.Clear();
        ReplaceCollection(CashReportRows, rows);
        ReplaceCollection(AccountantReportMetricCards, BuildCashMetricCards(rows));

        AccountantReportFilterSummary = BuildFilterSummary("Продавец", GetSelectedFilterName());
        AccountantReportStatusText = rows.Count == 0
            ? "По выбранным параметрам записи не найдены."
            : $"Строк в отчёте: {rows.Count}.";
    }

    private void ResetAccountantReportPreview(string message)
    {
        SalesBySellerReportRows.Clear();
        SalesByCategoryReportRows.Clear();
        CashReportRows.Clear();
        AccountantReportMetricCards.Clear();
        AccountantReportStatusText = message;
        AccountantReportFilterSummary = "Фильтры не применялись.";
        AccountantReportGeneratedAt = "Отчёт ещё не сформирован.";
        OnPropertyChanged(nameof(HasAccountantReportData));
    }

    private string GetSelectedFilterName()
    {
        return SelectedAccountantReport?.Key switch
        {
            AccountantReportKeys.SalesByCategory => SelectedReportCategory?.Name ?? "Все категории",
            AccountantReportKeys.SalesBySeller => SelectedReportSeller?.Name ?? "Все продавцы",
            AccountantReportKeys.Cash => SelectedReportSeller?.Name ?? "Все продавцы",
            _ => "Без дополнительного фильтра"
        };
    }

    private string BuildFilterSummary(string filterLabel, string filterValue)
    {
        return $"Период: {AccountantReportStartDate:dd.MM.yyyy} - {AccountantReportEndDate:dd.MM.yyyy}. {filterLabel}: {filterValue}.";
    }

    private static IReadOnlyList<LookupItem> BuildReportLookupItems(string allItemName, IEnumerable<LookupItem> items)
    {
        return [new LookupItem { Id = 0, Name = allItemName }, .. items];
    }

    private static IReadOnlyList<ReportMetricCard> BuildSalesBySellerMetricCards(IReadOnlyCollection<SalesBySellerReportRow> rows)
    {
        var totalOrders = rows.Sum(row => row.OrdersCountValue);
        var totalQuantity = rows.Sum(row => row.QuantityValue);
        var totalRevenue = rows.Sum(row => row.RevenueValue);

        return
        [
            new ReportMetricCard { Label = "Продавцов", Value = rows.Count.ToString() },
            new ReportMetricCard { Label = "Заказов", Value = totalOrders.ToString() },
            new ReportMetricCard { Label = "Продано", Value = totalQuantity.ToString() },
            new ReportMetricCard { Label = "Выручка", Value = FormatMoney(totalRevenue) }
        ];
    }

    private static IReadOnlyList<ReportMetricCard> BuildSalesByCategoryMetricCards(IReadOnlyCollection<SalesByCategoryReportRow> rows)
    {
        var totalProducts = rows.Sum(row => row.ProductCountValue);
        var totalQuantity = rows.Sum(row => row.QuantityValue);
        var totalRevenue = rows.Sum(row => row.RevenueValue);

        return
        [
            new ReportMetricCard { Label = "Категорий", Value = rows.Count.ToString() },
            new ReportMetricCard { Label = "Товаров", Value = totalProducts.ToString() },
            new ReportMetricCard { Label = "Продано", Value = totalQuantity.ToString() },
            new ReportMetricCard { Label = "Выручка", Value = FormatMoney(totalRevenue) }
        ];
    }

    private static IReadOnlyList<ReportMetricCard> BuildCashMetricCards(IReadOnlyCollection<CashReportRow> rows)
    {
        var totalOrders = rows.Sum(row => row.OrdersCountValue);
        var totalQuantity = rows.Sum(row => row.QuantityValue);
        var totalRevenue = rows.Sum(row => row.RevenueValue);

        return
        [
            new ReportMetricCard { Label = "Дней", Value = rows.Count.ToString() },
            new ReportMetricCard { Label = "Заказов", Value = totalOrders.ToString() },
            new ReportMetricCard { Label = "Продано", Value = totalQuantity.ToString() },
            new ReportMetricCard { Label = "Выручка", Value = FormatMoney(totalRevenue) }
        ];
    }

    private static string FormatMoney(decimal value) => $"{value:0.##} ₽";

    private static string FormatDecimal(decimal value) => value.ToString("0.##");

    private static void AppendCsvRow(StringBuilder builder, params string[] values)
    {
        builder.AppendLine(string.Join(";", values.Select(EscapeCsvValue)));
    }

    private static string EscapeCsvValue(string value)
    {
        var normalized = value.Replace("\"", "\"\"");
        return normalized.IndexOfAny([';', '"', '\r', '\n']) >= 0
            ? $"\"{normalized}\""
            : normalized;
    }

    private static void ReplaceCollection<T>(ObservableCollection<T> target, IEnumerable<T> items)
    {
        target.Clear();
        foreach (var item in items) target.Add(item);
    }

    private static IReadOnlyList<NavigationItem> BuildNavigation(UserRoleAccess roleAccess)
    {
        var items = new List<NavigationItem>();
        var sections = new HashSet<string>(StringComparer.Ordinal);

        static void AddNavigationItem(List<NavigationItem> target, HashSet<string> knownSections, NavigationItem item)
        {
            if (knownSections.Add(item.SectionKey))
            {
                target.Add(item);
            }
        }

        if (roleAccess.HasAccess(UserRoleAccess.Administrator))
        {
            AddNavigationItem(items, sections, new NavigationItem { SectionKey = "Users", Title = "Управление пользователями", IconGlyph = "\uE716" });
            AddNavigationItem(items, sections, new NavigationItem { SectionKey = "Roles", Title = "Роли", IconGlyph = "\uE902" });
            AddNavigationItem(items, sections, new NavigationItem { SectionKey = "Categories", Title = "Категории", IconGlyph = "\uE8FD" });
            AddNavigationItem(items, sections, new NavigationItem { SectionKey = "Products", Title = "Управление товарами", IconGlyph = "\uE14C" });
            AddNavigationItem(items, sections, new NavigationItem { SectionKey = "Suppliers", Title = "Управление поставщиками", IconGlyph = "\uE7BF" });
            AddNavigationItem(items, sections, new NavigationItem { SectionKey = "StatusOrders", Title = "Статусы заказов", IconGlyph = "\uE8B8" });
            AddNavigationItem(items, sections, new NavigationItem { SectionKey = "StatusSupplies", Title = "Статусы поставок", IconGlyph = "\uE8B8" });
            AddNavigationItem(items, sections, new NavigationItem { SectionKey = "Orders", Title = "Заказы", IconGlyph = "\uE8A1" });
            AddNavigationItem(items, sections, new NavigationItem { SectionKey = "OrderDetails", Title = "Состав заказов", IconGlyph = "\uE8A1" });
            AddNavigationItem(items, sections, new NavigationItem { SectionKey = "Notifications", Title = "Уведомления", IconGlyph = "\uE7F4", HasAttentionMark = true });
            AddNavigationItem(items, sections, new NavigationItem { SectionKey = "Supplies", Title = "Поставки", IconGlyph = "\uE7BF" });
            AddNavigationItem(items, sections, new NavigationItem { SectionKey = "SupplyDetails", Title = "Состав поставок", IconGlyph = "\uE8D2" });
            AddNavigationItem(items, sections, new NavigationItem { SectionKey = "Losses", Title = "Списания", IconGlyph = "\uE74D" });
        }

        if (roleAccess.HasAccess(UserRoleAccess.Manager))
        {
            AddNavigationItem(items, sections, new NavigationItem { SectionKey = "Products", Title = "Управление товарами", IconGlyph = "\uE14C" });
            AddNavigationItem(items, sections, new NavigationItem { SectionKey = "Categories", Title = "Категории", IconGlyph = "\uE8FD" });
            AddNavigationItem(items, sections, new NavigationItem { SectionKey = "Suppliers", Title = "Управление поставщиками", IconGlyph = "\uE7BF" });
            AddNavigationItem(items, sections, new NavigationItem { SectionKey = "Orders", Title = "Заказы", IconGlyph = "\uE8A1" });
            AddNavigationItem(items, sections, new NavigationItem { SectionKey = "Notifications", Title = "Уведомления", IconGlyph = "\uE7F4", HasAttentionMark = true });
            AddNavigationItem(items, sections, new NavigationItem { SectionKey = "Supplies", Title = "Поставки", IconGlyph = "\uE7BF" });
            AddNavigationItem(items, sections, new NavigationItem { SectionKey = "SupplyDetails", Title = "Состав поставок", IconGlyph = "\uE8D2" });
            AddNavigationItem(items, sections, new NavigationItem { SectionKey = "Losses", Title = "Списания", IconGlyph = "\uE74D" });
        }

        if (roleAccess.HasAccess(UserRoleAccess.Seller))
        {
            AddNavigationItem(items, sections, new NavigationItem { SectionKey = "Sales", Title = "Продажа", IconGlyph = "\uEAFD" });
            AddNavigationItem(items, sections, new NavigationItem { SectionKey = "Products", Title = "Товары", IconGlyph = "\uE14C" });
        }

        if (roleAccess.HasAccess(UserRoleAccess.Director)
            || roleAccess.HasAccess(UserRoleAccess.Accountant)
            || roleAccess.HasAccess(UserRoleAccess.Administrator))
        {
            AddNavigationItem(items, sections, new NavigationItem { SectionKey = "Reports", Title = "Отчёты", IconGlyph = "\uE9D2" });
        }

        if (roleAccess.HasAccess(UserRoleAccess.Administrator))
        {
            AddNavigationItem(items, sections, new NavigationItem { SectionKey = "Settings", Title = "Настройки", IconGlyph = "\uE713" });
        }

        return items;
    }

    private static IReadOnlyList<ReportOption> BuildAccountantReports() =>
    [
        new ReportOption
        {
            Key = AccountantReportKeys.SalesBySeller,
            Number = 1,
            Title = "Продажи по продавцам",
            Description = "Заказы, позиции, выручка и средний чек по продавцам."
        },
        new ReportOption
        {
            Key = AccountantReportKeys.SalesByCategory,
            Number = 2,
            Title = "Продажи по товарным группам",
            Description = "Выручка и продажи по категориям за выбранный период."
        },
        new ReportOption
        {
            Key = AccountantReportKeys.Cash,
            Number = 3,
            Title = "Кассовый отчёт",
            Description = "Выручка, заказы и средний чек по дням."
        }
    ];

    private static IReadOnlyList<AdminReportCard> BuildAdminReportCards() =>
    [
        new AdminReportCard
        {
            Key = ManagementReportKeys.Sales,
            Title = "По продажам",
            Description = "Продажи по дням и топ товаров.",
            AccentColor = "#E7C8B7"
        },
        new AdminReportCard
        {
            Key = ManagementReportKeys.Profitability,
            Title = "По прибыльности",
            Description = "Маржа и валовая прибыль.",
            AccentColor = "#CDEBDD"
        },
        new AdminReportCard
        {
            Key = ManagementReportKeys.Losses,
            Title = "По убыткам",
            Description = "Списания и возвраты.",
            AccentColor = "#F2D8D8"
        }
    ];

    private bool CanManageUsers => IsAdminRole;
    private bool CanManageStatuses => IsAdminRole;
    private bool CanManageCatalog => IsAdminRole || IsManagerRole;
    private bool CanManageSuppliers => IsAdminRole || IsManagerRole;
    private bool CanManageOrders => IsAdminRole || IsManagerRole;
    private bool CanManageNotifications => IsAdminRole || IsManagerRole;
    private bool CanManageSupplies => IsAdminRole || IsManagerRole;
    private bool CanManageLosses => IsAdminRole || IsManagerRole;
    private bool CanPerformSales => IsAdminRole || IsSellerRole;
    private bool CanViewProducts => IsAdminRole || IsManagerRole || IsSellerRole;
    private bool CanCreateCustomerReturns => IsAdminRole || IsManagerRole || IsSellerRole;

    private static void EnsurePermission(bool isAllowed, string message)
    {
        if (!isAllowed)
        {
            throw new InvalidOperationException(message);
        }
    }
}


using System.Collections.ObjectModel;
using System.Windows.Input;
using TeaShopManager.Models;

namespace TeaShopManager.ViewModels;

public sealed class MainWindowViewModel : ViewModelBase
{
    private readonly RelayCommand _signOutCommand;
    private NavigationItem? _selectedNavigationItem;
    private string _pageTitle = string.Empty;
    private string _currentSectionKey = string.Empty;

    public MainWindowViewModel(UserSession currentUser)
    {
        CurrentUser = currentUser;
        _signOutCommand = new RelayCommand(_ => SignOutRequested?.Invoke());

        NavigationItems = new ObservableCollection<NavigationItem>(BuildNavigation(currentUser.RoleName));
        Users = new ObservableCollection<UserPreviewItem>(BuildUsers());
        Categories = new ObservableCollection<CategoryPreviewItem>(BuildCategories());
        Roles = new ObservableCollection<RolePreviewItem>(BuildRoles());
        Products = new ObservableCollection<ProductPreviewItem>(BuildProducts());
        Suppliers = new ObservableCollection<SupplierPreviewItem>(BuildSuppliers());
        StatusOrders = new ObservableCollection<StatusPreviewItem>(BuildStatusOrders());
        StatusSupplies = new ObservableCollection<StatusPreviewItem>(BuildStatusSupplies());
        Orders = new ObservableCollection<OrderPreviewItem>(BuildOrders());
        OrderDetails = new ObservableCollection<OrderDetailPreviewItem>(BuildOrderDetails());
        Notifications = new ObservableCollection<NotificationPreviewItem>(BuildNotifications());
        Supplies = new ObservableCollection<SupplyPreviewItem>(BuildSupplies());
        SupplyDetails = new ObservableCollection<SupplyDetailPreviewItem>(BuildSupplyDetails());
        Losses = new ObservableCollection<LossPreviewItem>(BuildLosses());
        AccountantReports = new ObservableCollection<ReportOption>(BuildAccountantReports());
        AdminReportCards = new ObservableCollection<AdminReportCard>(BuildAdminReportCards());
        SaleLines = new ObservableCollection<SaleLinePreviewItem>(BuildSaleLines());
        QuickSaleItems = new ObservableCollection<QuickSaleItem>(BuildQuickSaleItems());

        SelectedNavigationItem = NavigationItems.FirstOrDefault();
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
            if (!SetProperty(ref _selectedNavigationItem, value) || value is null)
            {
                return;
            }

            CurrentSectionKey = value.SectionKey;
            PageTitle = value.Title;
        }
    }

    public string CurrentSectionKey
    {
        get => _currentSectionKey;
        private set => SetProperty(ref _currentSectionKey, value);
    }

    public string PageTitle
    {
        get => _pageTitle;
        private set => SetProperty(ref _pageTitle, value);
    }

    public string RoleTag
    {
        get
        {
            var role = CurrentUser.RoleName.ToLowerInvariant();
            if (role.Contains("менедж"))
            {
                return "MANAGER";
            }

            if (role.Contains("продав"))
            {
                return "SALES";
            }

            if (role.Contains("бухгалтер"))
            {
                return "ACCOUNTANT";
            }

            return "ADMIN";
        }
    }

    public string WelcomeTitle => CurrentUser.DisplayName;

    public string RoleCaption => $"Роль: {CurrentUser.RoleName}";

    public string CurrentOrderTitle => "Текущий заказ #2451";

    public string CurrentOrderDate => "14 Октября, 15:30";

    public string CurrentOrderTotal => "2 100 ₽";

    public bool IsAdminRole => RoleTag == "ADMIN";

    public bool IsAccountantRole => RoleTag == "ACCOUNTANT";

    public bool IsManagerRole => RoleTag == "MANAGER";

    public bool IsSellerRole => RoleTag == "SALES";

    public ICommand SignOutCommand => _signOutCommand;

    private static IReadOnlyList<NavigationItem> BuildNavigation(string roleName)
    {
        var role = roleName.ToLowerInvariant();

        if (role.Contains("менедж"))
        {
            return
            [
                new NavigationItem { SectionKey = "Products", Title = "Управление товарами", IconGlyph = "\uE14C" },
                new NavigationItem { SectionKey = "Orders", Title = "Заказы", IconGlyph = "\uE8A1" },
                new NavigationItem { SectionKey = "Suppliers", Title = "Управление поставщиками", IconGlyph = "\uE7BF" },
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
            return
            [
                new NavigationItem { SectionKey = "Reports", Title = "Отчёты", IconGlyph = "\uE9D2" }
            ];
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

    private static IReadOnlyList<UserPreviewItem> BuildUsers()
    {
        return
        [
            new UserPreviewItem { Login = "admin", FullName = "Смирнов Александр Петрович", RoleName = "Администратор/Директор", Status = "Активен" },
            new UserPreviewItem { Login = "manager", FullName = "Волкова Елена Сергеевна", RoleName = "Менеджер", Status = "Активен" },
            new UserPreviewItem { Login = "seller", FullName = "Орлова Мария Андреевна", RoleName = "Продавец-консультант", Status = "Активен" },
            new UserPreviewItem { Login = "accountant", FullName = "Кузнецова Ольга Ивановна", RoleName = "Бухгалтер", Status = "Активен" }
        ];
    }

    private static IReadOnlyList<CategoryPreviewItem> BuildCategories()
    {
        return
        [
            new CategoryPreviewItem { CategoryCode = "1", CategoryName = "Чай" },
            new CategoryPreviewItem { CategoryCode = "2", CategoryName = "Посуда" },
            new CategoryPreviewItem { CategoryCode = "3", CategoryName = "Аксессуары" }
        ];
    }

    private static IReadOnlyList<RolePreviewItem> BuildRoles()
    {
        return
        [
            new RolePreviewItem { RoleCode = "1", RoleName = "Администратор" },
            new RolePreviewItem { RoleCode = "2", RoleName = "Менеджер" },
            new RolePreviewItem { RoleCode = "3", RoleName = "Продавец" },
            new RolePreviewItem { RoleCode = "4", RoleName = "Бухгалтер" }
        ];
    }

    private static IReadOnlyList<ProductPreviewItem> BuildProducts()
    {
        return
        [
            new ProductPreviewItem { ProductName = "Молочный улун", CategoryName = "Чай", StockQuantity = "1500 г", MinStockQuantity = "500 г", ExpirationDate = "2026-12-31" },
            new ProductPreviewItem { ProductName = "Сенча", CategoryName = "Чай", StockQuantity = "980 г", MinStockQuantity = "300 г", ExpirationDate = "2026-10-15" },
            new ProductPreviewItem { ProductName = "Пуэр шу 7 лет", CategoryName = "Чай", StockQuantity = "450 г", MinStockQuantity = "200 г", ExpirationDate = "2028-01-15" },
            new ProductPreviewItem { ProductName = "Улун «Те Гуань Инь»", CategoryName = "Чай", StockQuantity = "80 г", MinStockQuantity = "100 г", ExpirationDate = "2025-03-20", IsCritical = true },
            new ProductPreviewItem { ProductName = "Чайник керамический 0.6 л", CategoryName = "Посуда", StockQuantity = "12 шт", MinStockQuantity = "5 шт", ExpirationDate = "-" }
        ];
    }

    private static IReadOnlyList<SupplierPreviewItem> BuildSuppliers()
    {
        return
        [
            new SupplierPreviewItem
            {
                SupplierName = "ООО «ЧайМир»",
                ContactPerson = "Иванов Сергей",
                Phone = "+7-921-111-11-11",
                Email = "sales@chaimir.ru",
                Address = "г. Санкт-Петербург, Невский пр., 1"
            },
            new SupplierPreviewItem
            {
                SupplierName = "АО «Восточный Чай»",
                ContactPerson = "Петрова Анна",
                Phone = "+7-921-222-22-22",
                Email = "info@easttea.ru",
                Address = "г. Москва, ул. Тверская, 13"
            },
            new SupplierPreviewItem
            {
                SupplierName = "ИП «Tea&Co»",
                ContactPerson = "Сидоров Олег",
                Phone = "+7-921-333-33-33",
                Email = "order@tea-co.ru",
                Address = "г. Казань, ул. Баумана, 5"
            }
        ];
    }

    private static IReadOnlyList<StatusPreviewItem> BuildStatusOrders()
    {
        return
        [
            new StatusPreviewItem { StatusCode = "1", StatusName = "Собирается" },
            new StatusPreviewItem { StatusCode = "2", StatusName = "Оплачен" },
            new StatusPreviewItem { StatusCode = "3", StatusName = "Отменён" }
        ];
    }

    private static IReadOnlyList<StatusPreviewItem> BuildStatusSupplies()
    {
        return
        [
            new StatusPreviewItem { StatusCode = "1", StatusName = "Новая" },
            new StatusPreviewItem { StatusCode = "2", StatusName = "В пути" },
            new StatusPreviewItem { StatusCode = "3", StatusName = "Принята" }
        ];
    }

    private static IReadOnlyList<OrderPreviewItem> BuildOrders()
    {
        return
        [
            new OrderPreviewItem { OrderCode = "#2451", OrderDate = "14.10.2026 15:30", StatusName = "Оплачен", TotalAmount = "2 100 ₽" },
            new OrderPreviewItem { OrderCode = "#2450", OrderDate = "14.10.2026 14:05", StatusName = "Собирается", TotalAmount = "1 580 ₽" },
            new OrderPreviewItem { OrderCode = "#2449", OrderDate = "14.10.2026 12:40", StatusName = "Отменён", TotalAmount = "950 ₽" }
        ];
    }

    private static IReadOnlyList<OrderDetailPreviewItem> BuildOrderDetails()
    {
        return
        [
            new OrderDetailPreviewItem { OrderCode = "#2451", ProductName = "Чай зелёный «Лунцзин»", Quantity = "50", Price = "12 ₽" },
            new OrderDetailPreviewItem { OrderCode = "#2451", ProductName = "Улун «Те Гуань Инь»", Quantity = "100", Price = "15 ₽" },
            new OrderDetailPreviewItem { OrderCode = "#2450", ProductName = "Пуэр шу 7 лет", Quantity = "80", Price = "14 ₽" }
        ];
    }

    private static IReadOnlyList<NotificationPreviewItem> BuildNotifications()
    {
        return
        [
            new NotificationPreviewItem
            {
                NotificationType = "Минимальный остаток",
                Message = "Товар «Улун Те Гуань Инь» опустился ниже минимального остатка.",
                CreatedAt = "14.10.2026 10:15",
                ReadState = "Нет"
            },
            new NotificationPreviewItem
            {
                NotificationType = "Срок годности",
                Message = "Проверьте актуальность партии товара «Молочный улун».",
                CreatedAt = "14.10.2026 09:40",
                ReadState = "Да"
            }
        ];
    }

    private static IReadOnlyList<SupplyPreviewItem> BuildSupplies()
    {
        return
        [
            new SupplyPreviewItem
            {
                SupplyCode = "301",
                CreationDate = "10.10.2026",
                DeliveryDate = "15.10.2026",
                StatusName = "В пути",
                SupplierName = "ООО «ЧайМир»",
                UserName = "manager"
            },
            new SupplyPreviewItem
            {
                SupplyCode = "302",
                CreationDate = "12.10.2026",
                DeliveryDate = "16.10.2026",
                StatusName = "Новая",
                SupplierName = "АО «Восточный Чай»",
                UserName = "manager"
            }
        ];
    }

    private static IReadOnlyList<SupplyDetailPreviewItem> BuildSupplyDetails()
    {
        return
        [
            new SupplyDetailPreviewItem
            {
                SupplyCode = "301",
                ProductName = "Молочный улун",
                Quantity = "1500",
                UnitPrice = "9.50 ₽",
                ProductionDate = "01.10.2026"
            },
            new SupplyDetailPreviewItem
            {
                SupplyCode = "301",
                ProductName = "Сенча",
                Quantity = "1200",
                UnitPrice = "8.90 ₽",
                ProductionDate = "03.10.2026"
            },
            new SupplyDetailPreviewItem
            {
                SupplyCode = "302",
                ProductName = "Чайник керамический 0.6 л",
                Quantity = "20",
                UnitPrice = "450 ₽",
                ProductionDate = "20.09.2026"
            }
        ];
    }

    private static IReadOnlyList<LossPreviewItem> BuildLosses()
    {
        return
        [
            new LossPreviewItem
            {
                LossCode = "41",
                ProductName = "Улун «Те Гуань Инь»",
                Reason = "Истечение срока годности",
                LossDate = "14.10.2026"
            },
            new LossPreviewItem
            {
                LossCode = "42",
                ProductName = "Чайник керамический 0.6 л",
                Reason = "Повреждение при транспортировке",
                LossDate = "11.10.2026"
            }
        ];
    }

    private static IReadOnlyList<ReportOption> BuildAccountantReports()
    {
        return
        [
            new ReportOption { Number = 1, Title = "Продажи по контрагентам" },
            new ReportOption { Number = 2, Title = "Продажи по товарным группам" },
            new ReportOption { Number = 3, Title = "Кассовый отчёт" }
        ];
    }

    private static IReadOnlyList<AdminReportCard> BuildAdminReportCards()
    {
        return
        [
            new AdminReportCard { Title = "По продажам", Description = "Аналитика общих продаж за выбранный период времени.", AccentColor = "#E7C8B7" },
            new AdminReportCard { Title = "По прибыльности", Description = "Оценка маржинальности товаров и чистой прибыли магазина.", AccentColor = "#CDEBDD" },
            new AdminReportCard { Title = "По убыткам", Description = "Анализ списаний, просроченной продукции и возвратов.", AccentColor = "#F2D8D8" }
        ];
    }

    private static IReadOnlyList<SaleLinePreviewItem> BuildSaleLines()
    {
        return
        [
            new SaleLinePreviewItem { ProductName = "Чай зелёный «Лунцзин»", Quantity = "50 г", Price = "12 ₽/гр", TotalAmount = "600 ₽" },
            new SaleLinePreviewItem { ProductName = "Улун «Те Гуань Инь»", Quantity = "100 г", Price = "15 ₽/гр", TotalAmount = "1 500 ₽" }
        ];
    }

    private static IReadOnlyList<QuickSaleItem> BuildQuickSaleItems()
    {
        return
        [
            new QuickSaleItem { Title = "Пакет бумажный (S)", Price = "15 ₽" },
            new QuickSaleItem { Title = "Дегустация (сет)", Price = "350 ₽" }
        ];
    }
}

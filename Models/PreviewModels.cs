using System.ComponentModel.DataAnnotations;

namespace TeaShopManager.Models;

public sealed class SummaryCard
{
    public string Title { get; init; } = string.Empty;

    public string Value { get; init; } = string.Empty;

    public string Caption { get; init; } = string.Empty;
}

public sealed class UserPreviewItem
{
    public int UserId { get; init; }

    [Display(Name = "Логин")]
    public string Login { get; init; } = string.Empty;

    public string PasswordHash { get; init; } = string.Empty;

    [Display(Name = "ФИО")]
    public string FullName { get; init; } = string.Empty;

    public short RoleId { get; init; }

    [Display(Name = "Роль")]
    public string RoleName { get; init; } = string.Empty;

    [Display(Name = "Статус")]
    public string Status { get; init; } = string.Empty;
}

public sealed class ProductPreviewItem
{
    public int ProductId { get; init; }

    [Display(Name = "Название товара")]
    public string ProductName { get; init; } = string.Empty;

    public decimal PriceValue { get; init; }

    [Display(Name = "Цена")]
    public string Price => $"{PriceValue:0.##} ₽";

    public string? Description { get; init; }

    public short ExpirationDays { get; init; }

    public short CategoryId { get; init; }

    [Display(Name = "Категория")]
    public string CategoryName { get; init; } = string.Empty;

    public int StockQuantityValue { get; init; }

    [Display(Name = "Остаток")]
    public string StockQuantity { get; init; } = string.Empty;

    public int MinStockQuantityValue { get; init; }

    [Display(Name = "Мин. остаток")]
    public string MinStockQuantity { get; init; } = string.Empty;

    [Display(Name = "Срок годности")]
    public string ExpirationDate { get; init; } = string.Empty;

    public bool IsCritical { get; init; }
}

public sealed class ProductMovementPreviewItem
{
    public int SupplyId { get; init; }

    public bool IsCustomerReturn { get; init; }

    public DateTime ReceivedDateValue { get; init; }

    public DateTime ProductionDateValue { get; init; }

    public decimal UnitPriceValue { get; init; }

    public int QuantityValue { get; init; }

    public int SoldQuantityValue { get; init; }

    public int RemainingQuantityValue { get; init; }

    [Display(Name = "Тип")]
    public string MovementType { get; init; } = string.Empty;

    [Display(Name = "Код документа")]
    public string DocumentCode { get; init; } = string.Empty;

    [Display(Name = "Поставщик / источник")]
    public string PartnerName { get; init; } = string.Empty;

    [Display(Name = "Дата приёмки")]
    public string ReceivedDate { get; init; } = string.Empty;

    [Display(Name = "Дата производства")]
    public string ProductionDate { get; init; } = string.Empty;

    [Display(Name = "Закупочная цена")]
    public string UnitPrice { get; init; } = string.Empty;

    [Display(Name = "Было в партии")]
    public string Quantity { get; init; } = string.Empty;

    [Display(Name = "Продано")]
    public string SoldQuantity { get; init; } = string.Empty;

    [Display(Name = "Осталось")]
    public string RemainingQuantity { get; init; } = string.Empty;
}

public sealed class SupplierPreviewItem
{
    public int SupplierId { get; init; }

    [Display(Name = "Название поставщика")]
    public string SupplierName { get; init; } = string.Empty;

    [Display(Name = "Контактное лицо")]
    public string ContactPerson { get; init; } = string.Empty;

    [Display(Name = "Телефон")]
    public string Phone { get; init; } = string.Empty;

    [Display(Name = "Email")]
    public string Email { get; init; } = string.Empty;

    [Display(Name = "Адрес")]
    public string Address { get; init; } = string.Empty;
}

public sealed class CategoryPreviewItem
{
    public short CategoryId { get; init; }

    [Display(Name = "Код категории")]
    public string CategoryCode { get; init; } = string.Empty;

    [Display(Name = "Название категории")]
    public string CategoryName { get; init; } = string.Empty;
}

public sealed class RolePreviewItem
{
    public short RoleId { get; init; }

    [Display(Name = "Код роли")]
    public string RoleCode { get; init; } = string.Empty;

    [Display(Name = "Название роли")]
    public string RoleName { get; init; } = string.Empty;
}

public sealed class StatusPreviewItem
{
    public int StatusId { get; init; }

    [Display(Name = "Код статуса")]
    public string StatusCode { get; init; } = string.Empty;

    [Display(Name = "Название статуса")]
    public string StatusName { get; init; } = string.Empty;
}

public sealed class OrderPreviewItem
{
    public int OrderId { get; init; }

    public int StatusId { get; init; }

    public int UserId { get; init; }

    public DateTime OrderDateValue { get; init; }

    [Display(Name = "Код заказа")]
    public string OrderCode { get; init; } = string.Empty;

    [Display(Name = "Дата заказа")]
    public string OrderDate { get; init; } = string.Empty;

    [Display(Name = "Статус заказа")]
    public string StatusName { get; init; } = string.Empty;

    [Display(Name = "Сумма")]
    public string TotalAmount { get; init; } = string.Empty;
}

public sealed class OrderDetailPreviewItem
{
    public int OrderId { get; init; }

    public int ProductId { get; init; }

    public int QuantityValue { get; init; }

    public decimal PriceValue { get; init; }

    [Display(Name = "Код заказа")]
    public string OrderCode { get; init; } = string.Empty;

    [Display(Name = "Товар")]
    public string ProductName { get; init; } = string.Empty;

    [Display(Name = "Количество")]
    public string Quantity { get; init; } = string.Empty;

    [Display(Name = "Цена")]
    public string Price { get; init; } = string.Empty;
}

public sealed class NotificationPreviewItem
{
    public int NotificationId { get; init; }

    public int ProductId { get; init; }

    public int UserId { get; init; }

    public bool IsRead { get; init; }

    public DateTime CreatedAtValue { get; init; }

    [Display(Name = "Тип уведомления")]
    public string NotificationType { get; init; } = string.Empty;

    [Display(Name = "Сообщение")]
    public string Message { get; init; } = string.Empty;

    [Display(Name = "Дата создания")]
    public string CreatedAt { get; init; } = string.Empty;

    [Display(Name = "Прочитано")]
    public string ReadState { get; init; } = string.Empty;

    public bool CanMarkAsRead => !IsRead;
}

public sealed class SupplyPreviewItem
{
    public int SupplyId { get; init; }

    public int StatusId { get; init; }

    public int SupplierId { get; init; }

    public int UserId { get; init; }

    public DateTime CreationDateValue { get; init; }

    public DateTime? DeliveryDateValue { get; init; }

    [Display(Name = "Код поставки")]
    public string SupplyCode { get; init; } = string.Empty;

    [Display(Name = "Дата создания")]
    public string CreationDate { get; init; } = string.Empty;

    [Display(Name = "Дата доставки")]
    public string DeliveryDate { get; init; } = string.Empty;

    [Display(Name = "Статус поставки")]
    public string StatusName { get; init; } = string.Empty;

    [Display(Name = "Поставщик")]
    public string SupplierName { get; init; } = string.Empty;

    [Display(Name = "Пользователь")]
    public string UserName { get; init; } = string.Empty;

    public bool CanModify { get; init; }

    public bool CanReceive { get; init; }

    public string ReceiveActionText => CanReceive ? "Принять" : "Принята";
}

public sealed class SupplyDetailPreviewItem
{
    public int SupplyId { get; init; }

    public int ProductId { get; init; }

    public int QuantityValue { get; init; }

    public decimal UnitPriceValue { get; init; }

    public DateTime ProductionDateValue { get; init; }

    [Display(Name = "Код поставки")]
    public string SupplyCode { get; init; } = string.Empty;

    [Display(Name = "Товар")]
    public string ProductName { get; init; } = string.Empty;

    [Display(Name = "Количество")]
    public string Quantity { get; init; } = string.Empty;

    [Display(Name = "Цена за единицу")]
    public string UnitPrice { get; init; } = string.Empty;

    [Display(Name = "Дата производства")]
    public string ProductionDate { get; init; } = string.Empty;

    public bool CanModify { get; init; }
}

public sealed class LossPreviewItem
{
    public int LossId { get; init; }

    public int ProductId { get; init; }

    public DateTime LossDateValue { get; init; }

    [Display(Name = "Код списания")]
    public string LossCode { get; init; } = string.Empty;

    [Display(Name = "Товар")]
    public string ProductName { get; init; } = string.Empty;

    [Display(Name = "Причина")]
    public string Reason { get; init; } = string.Empty;

    [Display(Name = "Дата списания")]
    public string LossDate { get; init; } = string.Empty;
}

public sealed class ReportOption
{
    public int Number { get; init; }

    public string Title { get; init; } = string.Empty;

    public string ButtonText { get; init; } = "Выгрузить CSV";
}

public sealed class AdminReportCard
{
    public string Title { get; init; } = string.Empty;

    public string Description { get; init; } = string.Empty;

    public string AccentColor { get; init; } = "#E6D3C2";
}

public sealed class QuickSaleItem
{
    public int? ProductId { get; init; }

    public string Title { get; init; } = string.Empty;

    public string Price { get; init; } = string.Empty;
}

public sealed class SaleLinePreviewItem
{
    public int ProductId { get; init; }

    public decimal UnitPriceValue { get; init; }

    public int QuantityValue { get; init; }

    [Display(Name = "Товар")]
    public string ProductName { get; init; } = string.Empty;

    [Display(Name = "Кол-во")]
    public string Quantity { get; init; } = string.Empty;

    [Display(Name = "Цена")]
    public string Price { get; init; } = string.Empty;

    [Display(Name = "Всего")]
    public string TotalAmount { get; init; } = string.Empty;
}

public sealed class LookupItem
{
    public int Id { get; init; }

    public string Name { get; init; } = string.Empty;
}

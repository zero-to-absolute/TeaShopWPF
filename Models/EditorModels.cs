namespace TeaShopManager.Models;

public sealed class UserEditorModel
{
    public int UserId { get; set; }

    public string Login { get; set; } = string.Empty;

    public string FullName { get; set; } = string.Empty;

    public short RoleId { get; set; }

    public string Password { get; set; } = string.Empty;
}

public sealed class CategoryEditorModel
{
    public short CategoryId { get; set; }

    public string CategoryName { get; set; } = string.Empty;
}

public sealed class SupplierEditorModel
{
    public int SupplierId { get; set; }

    public string SupplierName { get; set; } = string.Empty;

    public string ContactPerson { get; set; } = string.Empty;

    public string Phone { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public string Address { get; set; } = string.Empty;
}

public sealed class ProductEditorModel
{
    public int ProductId { get; set; }

    public string ProductName { get; set; } = string.Empty;

    public decimal Price { get; set; }

    public int MinStockQuantity { get; set; }

    public string Description { get; set; } = string.Empty;

    public short ExpirationDays { get; set; }

    public short CategoryId { get; set; }
}

public sealed class RoleEditorModel
{
    public short RoleId { get; set; }

    public string RoleName { get; set; } = string.Empty;
}

public sealed class StatusEditorModel
{
    public int StatusId { get; set; }

    public string StatusName { get; set; } = string.Empty;
}

public sealed class OrderEditorModel
{
    public int OrderId { get; set; }

    public DateTime OrderDate { get; set; } = DateTime.Now;

    public int StatusId { get; set; }

    public int UserId { get; set; }
}

public sealed class OrderDetailEditorModel
{
    public int OriginalOrderId { get; set; }

    public int OriginalProductId { get; set; }

    public int OrderId { get; set; }

    public int ProductId { get; set; }

    public int Quantity { get; set; }

    public decimal Price { get; set; }
}

public sealed class NotificationEditorModel
{
    public int NotificationId { get; set; }

    public string NotificationType { get; set; } = string.Empty;

    public string Message { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.Now;

    public int ProductId { get; set; }

    public int UserId { get; set; }

    public bool IsRead { get; set; }
}

public sealed class SupplyEditorModel
{
    public int SupplyId { get; set; }

    public DateTime CreationDate { get; set; } = DateTime.Today;

    public DateTime? DeliveryDate { get; set; }

    public int StatusId { get; set; }

    public int SupplierId { get; set; }

    public int UserId { get; set; }
}

public sealed class SupplyDetailEditorModel
{
    public int OriginalSupplyId { get; set; }

    public int OriginalProductId { get; set; }

    public int SupplyId { get; set; }

    public int ProductId { get; set; }

    public int Quantity { get; set; }

    public decimal UnitPrice { get; set; }

    public DateTime ProductionDate { get; set; } = DateTime.Today;
}

public sealed class CustomerReturnEditorModel
{
    public int ProductId { get; set; }

    public string ProductName { get; set; } = string.Empty;

    public string CategoryName { get; set; } = string.Empty;

    public DateTime ReturnDate { get; set; } = DateTime.Today;

    public int Quantity { get; set; }

    public decimal UnitPrice { get; set; }

    public DateTime ProductionDate { get; set; } = DateTime.Today;
}

public sealed class LossEditorModel
{
    public int LossId { get; set; }

    public int ProductId { get; set; }

    public string Reason { get; set; } = string.Empty;

    public DateTime LossDate { get; set; } = DateTime.Today;
}

public sealed class SaleDraftLine
{
    public int ProductId { get; set; }

    public string ProductName { get; set; } = string.Empty;

    public int Quantity { get; set; }

    public decimal UnitPrice { get; set; }
}

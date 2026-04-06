using System.Windows;
using TeaShopManager.Models;

namespace TeaShopManager.Views;

public partial class SupplyEditorWindow : Window
{
    public SupplyEditorWindow(SupplyEditorModel model, IReadOnlyList<LookupItem> statusItems, IReadOnlyList<LookupItem> supplierItems, IReadOnlyList<LookupItem> userItems)
    {
        InitializeComponent();
        MaxWidth = SystemParameters.WorkArea.Width - 40;
        MaxHeight = SystemParameters.WorkArea.Height - 40;
        Loaded += (_, _) => SizeToContent = System.Windows.SizeToContent.Manual;
        WindowTitle = model.SupplyId == 0 ? "Новая поставка" : "Редактирование поставки";
        StatusItems = statusItems;
        SupplierItems = supplierItems;
        UserItems = userItems;
        ResultModel = model;
        DataContext = this;

        CreationDatePicker.SelectedDate = model.CreationDate == default ? DateTime.Today : model.CreationDate;
        DeliveryDatePicker.SelectedDate = model.DeliveryDate;
        StatusComboBox.SelectedValue = model.StatusId == 0 && statusItems.Count > 0 ? statusItems[0].Id : model.StatusId;
        SupplierComboBox.SelectedValue = model.SupplierId == 0 && supplierItems.Count > 0 ? supplierItems[0].Id : model.SupplierId;
        UserComboBox.SelectedValue = model.UserId == 0 && userItems.Count > 0 ? userItems[0].Id : model.UserId;
    }

    public string WindowTitle { get; }
    public IReadOnlyList<LookupItem> StatusItems { get; }
    public IReadOnlyList<LookupItem> SupplierItems { get; }
    public IReadOnlyList<LookupItem> UserItems { get; }
    public SupplyEditorModel ResultModel { get; }

    private void SaveButton_OnClick(object sender, RoutedEventArgs e)
    {
        if (CreationDatePicker.SelectedDate is null || StatusComboBox.SelectedValue is null || SupplierComboBox.SelectedValue is null || UserComboBox.SelectedValue is null)
        {
            MessageBox.Show("Заполните данные поставки.", "TeaShopManager", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        ResultModel.CreationDate = CreationDatePicker.SelectedDate.Value;
        ResultModel.DeliveryDate = DeliveryDatePicker.SelectedDate;
        ResultModel.StatusId = Convert.ToInt32(StatusComboBox.SelectedValue);
        ResultModel.SupplierId = Convert.ToInt32(SupplierComboBox.SelectedValue);
        ResultModel.UserId = Convert.ToInt32(UserComboBox.SelectedValue);
        DialogResult = true;
    }

    private void CancelButton_OnClick(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
    }
}

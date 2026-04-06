using System.Windows;
using TeaShopManager.Models;

namespace TeaShopManager.Views;

public partial class OrderEditorWindow : Window
{
    public OrderEditorWindow(OrderEditorModel model, IReadOnlyList<LookupItem> statusItems, IReadOnlyList<LookupItem> userItems)
    {
        InitializeComponent();
        MaxWidth = SystemParameters.WorkArea.Width - 40;
        MaxHeight = SystemParameters.WorkArea.Height - 40;
        Loaded += (_, _) => SizeToContent = System.Windows.SizeToContent.Manual;
        WindowTitle = model.OrderId == 0 ? "Новый заказ" : "Редактирование заказа";
        StatusItems = statusItems;
        UserItems = userItems;
        ResultModel = new OrderEditorModel
        {
            OrderId = model.OrderId,
            OrderDate = model.OrderDate,
            StatusId = model.StatusId,
            UserId = model.UserId
        };
        DataContext = this;

        OrderDatePicker.SelectedDate = model.OrderDate == default ? DateTime.Today : model.OrderDate;
        StatusComboBox.SelectedValue = model.StatusId == 0 && statusItems.Count > 0 ? statusItems[0].Id : model.StatusId;
        UserComboBox.SelectedValue = model.UserId == 0 && userItems.Count > 0 ? userItems[0].Id : model.UserId;
    }

    public string WindowTitle { get; }
    public IReadOnlyList<LookupItem> StatusItems { get; }
    public IReadOnlyList<LookupItem> UserItems { get; }
    public OrderEditorModel ResultModel { get; }

    private void SaveButton_OnClick(object sender, RoutedEventArgs e)
    {
        if (OrderDatePicker.SelectedDate is null || StatusComboBox.SelectedValue is null || UserComboBox.SelectedValue is null)
        {
            MessageBox.Show("Заполните данные заказа.", "TeaShopManager", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        ResultModel.OrderDate = OrderDatePicker.SelectedDate.Value;
        ResultModel.StatusId = Convert.ToInt32(StatusComboBox.SelectedValue);
        ResultModel.UserId = Convert.ToInt32(UserComboBox.SelectedValue);
        DialogResult = true;
    }

    private void CancelButton_OnClick(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
    }
}

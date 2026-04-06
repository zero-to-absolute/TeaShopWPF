using System.Windows;
using TeaShopManager.Models;

namespace TeaShopManager.Views;

public partial class NotificationEditorWindow : Window
{
    public NotificationEditorWindow(NotificationEditorModel model, IReadOnlyList<LookupItem> productItems, IReadOnlyList<LookupItem> userItems)
    {
        InitializeComponent();
        MaxWidth = SystemParameters.WorkArea.Width - 40;
        MaxHeight = SystemParameters.WorkArea.Height - 40;
        Loaded += (_, _) => SizeToContent = System.Windows.SizeToContent.Manual;
        WindowTitle = model.NotificationId == 0 ? "Новое уведомление" : "Редактирование уведомления";
        ProductItems = productItems;
        UserItems = userItems;
        ResultModel = model;
        DataContext = this;

        TypeTextBox.Text = model.NotificationType;
        MessageTextBox.Text = model.Message;
        CreatedDatePicker.SelectedDate = model.CreatedAt == default ? DateTime.Today : model.CreatedAt;
        ProductComboBox.SelectedValue = model.ProductId == 0 && productItems.Count > 0 ? productItems[0].Id : model.ProductId;
        UserComboBox.SelectedValue = model.UserId == 0 && userItems.Count > 0 ? userItems[0].Id : model.UserId;
        IsReadCheckBox.IsChecked = model.IsRead;
    }

    public string WindowTitle { get; }
    public IReadOnlyList<LookupItem> ProductItems { get; }
    public IReadOnlyList<LookupItem> UserItems { get; }
    public NotificationEditorModel ResultModel { get; }

    private void SaveButton_OnClick(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(TypeTextBox.Text) || string.IsNullOrWhiteSpace(MessageTextBox.Text) || CreatedDatePicker.SelectedDate is null || ProductComboBox.SelectedValue is null || UserComboBox.SelectedValue is null)
        {
            MessageBox.Show("Заполните данные уведомления.", "TeaShopManager", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        ResultModel.NotificationType = TypeTextBox.Text.Trim();
        ResultModel.Message = MessageTextBox.Text.Trim();
        ResultModel.CreatedAt = CreatedDatePicker.SelectedDate.Value;
        ResultModel.ProductId = Convert.ToInt32(ProductComboBox.SelectedValue);
        ResultModel.UserId = Convert.ToInt32(UserComboBox.SelectedValue);
        ResultModel.IsRead = IsReadCheckBox.IsChecked == true;
        DialogResult = true;
    }

    private void CancelButton_OnClick(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
    }
}

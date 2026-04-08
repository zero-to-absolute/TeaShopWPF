using System.Windows;
using TeaShopManager.Models;

namespace TeaShopManager.Views;

public partial class LossEditorWindow : Window
{
    public LossEditorWindow(LossEditorModel model, IReadOnlyList<LookupItem> productItems)
    {
        InitializeComponent();
        MaxWidth = SystemParameters.WorkArea.Width - 40;
        MaxHeight = SystemParameters.WorkArea.Height - 40;
        Loaded += (_, _) => SizeToContent = System.Windows.SizeToContent.Manual;
        WindowTitle = model.LossId == 0 ? "Новое списание" : "Редактирование списания";
        ProductItems = productItems;
        ResultModel = model;
        DataContext = this;

        ProductComboBox.SelectedValue = model.ProductId == 0 && productItems.Count > 0 ? productItems[0].Id : model.ProductId;
        QuantityTextBox.Text = model.Quantity <= 0 ? "1" : model.Quantity.ToString();
        ReasonTextBox.Text = model.Reason;
        LossDatePicker.SelectedDate = model.LossDate == default ? DateTime.Today : model.LossDate;
    }

    public string WindowTitle { get; }
    public IReadOnlyList<LookupItem> ProductItems { get; }
    public LossEditorModel ResultModel { get; }

    private void SaveButton_OnClick(object sender, RoutedEventArgs e)
    {
        if (ProductComboBox.SelectedValue is null || string.IsNullOrWhiteSpace(ReasonTextBox.Text) || LossDatePicker.SelectedDate is null)
        {
            MessageBox.Show("Заполните данные списания.", "TeaShopManager", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (!int.TryParse(QuantityTextBox.Text, out var quantity) || quantity <= 0)
        {
            MessageBox.Show("Количество списания должно быть больше нуля.", "TeaShopManager", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        ResultModel.ProductId = Convert.ToInt32(ProductComboBox.SelectedValue);
        ResultModel.Quantity = quantity;
        ResultModel.Reason = ReasonTextBox.Text.Trim();
        ResultModel.LossDate = LossDatePicker.SelectedDate.Value;
        DialogResult = true;
    }

    private void CancelButton_OnClick(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
    }
}

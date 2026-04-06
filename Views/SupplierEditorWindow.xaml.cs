using System.Text.RegularExpressions;
using System.Windows;
using TeaShopManager.Models;

namespace TeaShopManager.Views;

public partial class SupplierEditorWindow : Window
{
    private static readonly Regex PhoneRegex = new(@"^\+?[0-9][0-9\s\-\(\)]*$", RegexOptions.Compiled);
    private static readonly Regex EmailRegex = new(@"^[^@\s]+@[^@\s]+\.[^@\s]+$", RegexOptions.Compiled);

    public SupplierEditorWindow(SupplierEditorModel model)
    {
        InitializeComponent();
        MaxWidth = SystemParameters.WorkArea.Width - 40;
        MaxHeight = SystemParameters.WorkArea.Height - 40;
        Loaded += (_, _) => SizeToContent = System.Windows.SizeToContent.Manual;
        WindowTitle = model.SupplierId == 0 ? "Новый поставщик" : "Редактирование поставщика";
        ResultModel = new SupplierEditorModel
        {
            SupplierId = model.SupplierId,
            SupplierName = model.SupplierName,
            ContactPerson = model.ContactPerson,
            Phone = model.Phone,
            Email = model.Email,
            Address = model.Address
        };
        DataContext = this;

        SupplierNameTextBox.Text = model.SupplierName;
        ContactPersonTextBox.Text = model.ContactPerson;
        PhoneTextBox.Text = model.Phone;
        EmailTextBox.Text = model.Email;
        AddressTextBox.Text = model.Address;
    }

    public string WindowTitle { get; }

    public SupplierEditorModel ResultModel { get; }

    private void SaveButton_OnClick(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(SupplierNameTextBox.Text) ||
            string.IsNullOrWhiteSpace(ContactPersonTextBox.Text) ||
            string.IsNullOrWhiteSpace(PhoneTextBox.Text) ||
            string.IsNullOrWhiteSpace(AddressTextBox.Text))
        {
            MessageBox.Show("Заполните обязательные поля поставщика.", "TeaShopManager", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (!PhoneRegex.IsMatch(PhoneTextBox.Text.Trim()))
        {
            MessageBox.Show("Введите корректный телефон.", "TeaShopManager", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (!string.IsNullOrWhiteSpace(EmailTextBox.Text) && !EmailRegex.IsMatch(EmailTextBox.Text.Trim()))
        {
            MessageBox.Show("Введите корректный email.", "TeaShopManager", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        ResultModel.SupplierName = SupplierNameTextBox.Text.Trim();
        ResultModel.ContactPerson = ContactPersonTextBox.Text.Trim();
        ResultModel.Phone = PhoneTextBox.Text.Trim();
        ResultModel.Email = EmailTextBox.Text.Trim();
        ResultModel.Address = AddressTextBox.Text.Trim();
        DialogResult = true;
    }

    private void CancelButton_OnClick(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
    }
}

using System.Windows;
using TeaShopManager.Models;

namespace TeaShopManager.Views;

public partial class UserEditorWindow : Window
{
    public UserEditorWindow(UserEditorModel model, IReadOnlyList<LookupItem> roleItems)
    {
        InitializeComponent();
        MaxWidth = SystemParameters.WorkArea.Width - 40;
        MaxHeight = SystemParameters.WorkArea.Height - 40;
        Loaded += (_, _) => SizeToContent = System.Windows.SizeToContent.Manual;
        DataContext = this;
        WindowTitle = model.UserId == 0 ? "Новый пользователь" : "Редактирование пользователя";
        RoleItems = roleItems;
        ResultModel = new UserEditorModel
        {
            UserId = model.UserId,
            Login = model.Login,
            FullName = model.FullName,
            RoleId = model.RoleId
        };

        LoginTextBox.Text = model.Login;
        FullNameTextBox.Text = model.FullName;
        RoleComboBox.SelectedValue = model.RoleId == 0 && roleItems.Count > 0 ? roleItems[0].Id : model.RoleId;
    }

    public string WindowTitle { get; }

    public IReadOnlyList<LookupItem> RoleItems { get; }

    public UserEditorModel ResultModel { get; }

    private void SaveButton_OnClick(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(LoginTextBox.Text))
        {
            MessageBox.Show("Введите логин.", "TeaShopManager", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (RoleComboBox.SelectedValue is null)
        {
            MessageBox.Show("Выберите роль.", "TeaShopManager", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (ResultModel.UserId == 0 && string.IsNullOrWhiteSpace(PasswordBox.Password))
        {
            MessageBox.Show("Для нового пользователя нужно задать пароль.", "TeaShopManager", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        ResultModel.Login = LoginTextBox.Text.Trim();
        ResultModel.FullName = FullNameTextBox.Text.Trim();
        ResultModel.RoleId = Convert.ToInt16(RoleComboBox.SelectedValue);
        ResultModel.Password = PasswordBox.Password;
        DialogResult = true;
    }

    private void CancelButton_OnClick(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
    }
}

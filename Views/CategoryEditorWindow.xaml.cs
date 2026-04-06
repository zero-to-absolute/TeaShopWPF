using System.Windows;
using TeaShopManager.Models;

namespace TeaShopManager.Views;

public partial class CategoryEditorWindow : Window
{
    public CategoryEditorWindow(CategoryEditorModel model)
    {
        InitializeComponent();
        MaxWidth = SystemParameters.WorkArea.Width - 40;
        MaxHeight = SystemParameters.WorkArea.Height - 40;
        Loaded += (_, _) => SizeToContent = System.Windows.SizeToContent.Manual;
        WindowTitle = model.CategoryId == 0 ? "Новая категория" : "Редактирование категории";
        ResultModel = new CategoryEditorModel
        {
            CategoryId = model.CategoryId,
            CategoryName = model.CategoryName
        };
        DataContext = this;
        CategoryNameTextBox.Text = model.CategoryName;
    }

    public string WindowTitle { get; }

    public CategoryEditorModel ResultModel { get; }

    private void SaveButton_OnClick(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(CategoryNameTextBox.Text))
        {
            MessageBox.Show("Введите название категории.", "TeaShopManager", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        ResultModel.CategoryName = CategoryNameTextBox.Text.Trim();
        DialogResult = true;
    }

    private void CancelButton_OnClick(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
    }
}

using System.Windows;

namespace TeaShopManager.Views;

public partial class SimpleNameEditorWindow : Window
{
    public SimpleNameEditorWindow(string windowTitle, string fieldLabel, string initialValue)
    {
        InitializeComponent();
        MaxWidth = SystemParameters.WorkArea.Width - 40;
        MaxHeight = SystemParameters.WorkArea.Height - 40;
        Loaded += (_, _) => SizeToContent = System.Windows.SizeToContent.Manual;
        WindowTitle = windowTitle;
        FieldLabel = fieldLabel;
        DataContext = this;
        ValueTextBox.Text = initialValue;
    }

    public string WindowTitle { get; }

    public string FieldLabel { get; }

    public string ResultValue { get; private set; } = string.Empty;

    private void SaveButton_OnClick(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(ValueTextBox.Text))
        {
            MessageBox.Show("Заполните значение.", "TeaShopManager", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        ResultValue = ValueTextBox.Text.Trim();
        DialogResult = true;
    }

    private void CancelButton_OnClick(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
    }
}

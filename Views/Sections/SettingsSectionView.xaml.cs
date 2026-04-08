using System.Windows;

namespace TeaShopManager.Views.Sections;

public partial class SettingsSectionView : SectionViewBase
{
    public SettingsSectionView()
    {
        InitializeComponent();
    }

    private void SaveSettingsButton_OnClick(object sender, RoutedEventArgs e)
    {
        ExecuteWithHandling(() =>
        {
            var vm = RequireViewModel();
            vm.SaveApplicationSettings();
        });
    }
}

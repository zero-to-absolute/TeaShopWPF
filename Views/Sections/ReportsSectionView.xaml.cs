using System.IO;
using System.Text;
using System.Windows;
using Microsoft.Win32;
using TeaShopManager.Models;
using TeaShopManager.Views;

namespace TeaShopManager.Views.Sections;

public partial class ReportsSectionView : SectionViewBase
{
    public ReportsSectionView()
    {
        InitializeComponent();
    }

    private void GenerateAccountantReportButton_OnClick(object sender, RoutedEventArgs e)
    {
        ExecuteWithHandling(() =>
        {
            var vm = RequireViewModel();
            vm.GenerateAccountantReport();
        });
    }

    private void ExportAccountantReportButton_OnClick(object sender, RoutedEventArgs e)
    {
        ExecuteWithHandling(() =>
        {
            var vm = RequireViewModel();
            var dialog = new SaveFileDialog
            {
                Title = "Сохранение бухгалтерского отчёта",
                Filter = "CSV files (*.csv)|*.csv|All files (*.*)|*.*",
                DefaultExt = ".csv",
                AddExtension = true,
                OverwritePrompt = true,
                FileName = vm.GetAccountantReportFileName()
            };

            if (dialog.ShowDialog(OwnerWindow) != true)
            {
                return;
            }

            File.WriteAllText(dialog.FileName, vm.GetAccountantReportCsvContent(), new UTF8Encoding(true));
            MessageBox.Show(OwnerWindow, $"Отчёт сохранён в файл:\n{dialog.FileName}", "TeaShopManager", MessageBoxButton.OK, MessageBoxImage.Information);
        });
    }

    private void OpenManagementReportButton_OnClick(object sender, RoutedEventArgs e)
    {
        ExecuteWithHandling(() =>
        {
            var vm = RequireViewModel();
            if (((FrameworkElement)sender).Tag is not AdminReportCard card)
            {
                return;
            }

            var window = new ManagementReportWindow(vm, card) { Owner = OwnerWindow };
            window.ShowDialog();
        });
    }
}

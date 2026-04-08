using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;
using System.Windows;
using Microsoft.Win32;
using TeaShopManager.Models;
using TeaShopManager.ViewModels;

namespace TeaShopManager.Views;

public partial class ManagementReportWindow : Window, INotifyPropertyChanged
{
    private readonly ShellViewModel _shellViewModel;
    private readonly AdminReportCard _reportCard;
    private LookupItem? _selectedCategory;
    private DateTime _reportStartDate = new(DateTime.Today.Year, DateTime.Today.Month, 1);
    private DateTime _reportEndDate = DateTime.Today;
    private string _filterSummary = string.Empty;
    private string _generatedAtText = "Отчёт ещё не сформирован.";
    private string _statusText = "Выберите период и нажмите «Сформировать».";

    public ManagementReportWindow(ShellViewModel shellViewModel, AdminReportCard reportCard)
    {
        InitializeComponent();
        _shellViewModel = shellViewModel;
        _reportCard = reportCard;

        CategoryOptions = [];
        MetricCards = [];
        SalesDailyRows = [];
        TopProductRows = [];
        ProfitabilityRows = [];
        LossRows = [];
        CustomerReturnRows = [];

        WindowTitle = $"Управленческий отчёт: {reportCard.Title}";
        WindowDescription = reportCard.Description;

        LoadLookups();
        DataContext = this;
        GenerateReport();
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public string WindowTitle { get; }
    public string WindowDescription { get; }
    public ObservableCollection<LookupItem> CategoryOptions { get; }
    public ObservableCollection<ReportMetricCard> MetricCards { get; }
    public ObservableCollection<CashReportRow> SalesDailyRows { get; }
    public ObservableCollection<ManagementSalesTopProductRow> TopProductRows { get; }
    public ObservableCollection<ProfitabilityReportRow> ProfitabilityRows { get; }
    public ObservableCollection<LossAnalyticsReportRow> LossRows { get; }
    public ObservableCollection<CustomerReturnAnalyticsRow> CustomerReturnRows { get; }

    public bool IsSalesReport => _reportCard.Key == ManagementReportKeys.Sales;
    public bool IsProfitabilityReport => _reportCard.Key == ManagementReportKeys.Profitability;
    public bool IsLossesReport => _reportCard.Key == ManagementReportKeys.Losses;
    public bool ShowCategoryFilter => IsProfitabilityReport;

    public DateTime ReportStartDate
    {
        get => _reportStartDate;
        set => SetProperty(ref _reportStartDate, value.Date);
    }

    public DateTime ReportEndDate
    {
        get => _reportEndDate;
        set => SetProperty(ref _reportEndDate, value.Date);
    }

    public LookupItem? SelectedCategory
    {
        get => _selectedCategory;
        set => SetProperty(ref _selectedCategory, value);
    }

    public string FilterSummary
    {
        get => _filterSummary;
        private set => SetProperty(ref _filterSummary, value);
    }

    public string GeneratedAtText
    {
        get => _generatedAtText;
        private set => SetProperty(ref _generatedAtText, value);
    }

    public string StatusText
    {
        get => _statusText;
        private set => SetProperty(ref _statusText, value);
    }

    public bool HasReportData => MetricCards.Count > 0
        && (IsSalesReport && (SalesDailyRows.Count > 0 || TopProductRows.Count > 0)
            || IsProfitabilityReport && ProfitabilityRows.Count > 0
            || IsLossesReport && (LossRows.Count > 0 || CustomerReturnRows.Count > 0));

    private void GenerateButton_OnClick(object sender, RoutedEventArgs e)
    {
        try
        {
            GenerateReport();
        }
        catch (Exception exception)
        {
            MessageBox.Show(exception.Message, "TeaShopManager", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void ExportButton_OnClick(object sender, RoutedEventArgs e)
    {
        try
        {
            if (!HasReportData)
            {
                throw new InvalidOperationException("Сначала сформируйте отчёт и убедитесь, что в нём есть данные для выгрузки.");
            }

            var dialog = new SaveFileDialog
            {
                Title = "Сохранение управленческого отчёта",
                Filter = "CSV files (*.csv)|*.csv|All files (*.*)|*.*",
                DefaultExt = ".csv",
                AddExtension = true,
                OverwritePrompt = true,
                FileName = GetFileName()
            };

            if (dialog.ShowDialog(this) != true)
            {
                return;
            }

            File.WriteAllText(dialog.FileName, BuildCsvContent(), new UTF8Encoding(true));
            MessageBox.Show($"Отчёт сохранён в файл:\n{dialog.FileName}", "TeaShopManager", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception exception)
        {
            MessageBox.Show(exception.Message, "TeaShopManager", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void CloseButton_OnClick(object sender, RoutedEventArgs e)
    {
        Close();
    }

    private void GenerateReport()
    {
        if (ReportStartDate.Date > ReportEndDate.Date)
        {
            throw new InvalidOperationException("Дата начала периода не может быть позже даты окончания.");
        }

        switch (_reportCard.Key)
        {
            case ManagementReportKeys.Sales:
                GenerateSalesReport();
                break;
            case ManagementReportKeys.Profitability:
                GenerateProfitabilityReport();
                break;
            case ManagementReportKeys.Losses:
                GenerateLossesReport();
                break;
            default:
                throw new InvalidOperationException("Неизвестный тип управленческого отчёта.");
        }

        GeneratedAtText = $"Сформирован: {DateTime.Now:dd.MM.yyyy HH:mm}";
        OnPropertyChanged(nameof(HasReportData));
    }

    private void GenerateSalesReport()
    {
        var dailyRows = _shellViewModel.GetManagementSalesDailyReport(ReportStartDate, ReportEndDate);
        var topRows = _shellViewModel.GetManagementSalesTopProducts(ReportStartDate, ReportEndDate, 10);

        ReplaceCollection(SalesDailyRows, dailyRows);
        ReplaceCollection(TopProductRows, topRows);
        ProfitabilityRows.Clear();
        LossRows.Clear();
        CustomerReturnRows.Clear();

        var totalOrders = dailyRows.Sum(row => row.OrdersCountValue);
        var totalQuantity = dailyRows.Sum(row => row.QuantityValue);
        var totalRevenue = dailyRows.Sum(row => row.RevenueValue);
        var averageCheck = totalOrders == 0 ? 0m : totalRevenue / totalOrders;

        ReplaceCollection(MetricCards,
        [
            new ReportMetricCard { Label = "Заказов", Value = totalOrders.ToString() },
            new ReportMetricCard { Label = "Продано", Value = totalQuantity.ToString() },
            new ReportMetricCard { Label = "Выручка", Value = FormatMoney(totalRevenue) },
            new ReportMetricCard { Label = "Средний чек", Value = FormatMoney(averageCheck) }
        ]);

        FilterSummary = $"Период: {ReportStartDate:dd.MM.yyyy} - {ReportEndDate:dd.MM.yyyy}. Продажи по дням и топ-10 товаров.";
        StatusText = dailyRows.Count == 0 && topRows.Count == 0
            ? "По выбранному периоду продажи не найдены."
            : $"Дней в выборке: {dailyRows.Count}. Товаров в рейтинге: {topRows.Count}.";
    }

    private void GenerateProfitabilityReport()
    {
        var categoryId = SelectedCategory is { Id: > 0 } ? Convert.ToInt16(SelectedCategory.Id) : (short?)null;
        var rows = _shellViewModel.GetManagementProfitabilityReport(ReportStartDate, ReportEndDate, categoryId);

        SalesDailyRows.Clear();
        TopProductRows.Clear();
        ReplaceCollection(ProfitabilityRows, rows);
        LossRows.Clear();
        CustomerReturnRows.Clear();

        var totalRevenue = rows.Sum(row => row.RevenueValue);
        var totalCost = rows.Sum(row => row.EstimatedCostValue);
        var totalProfit = rows.Sum(row => row.GrossProfitValue);
        var marginPercent = totalRevenue == 0 ? 0m : totalProfit * 100m / totalRevenue;

        ReplaceCollection(MetricCards,
        [
            new ReportMetricCard { Label = "Товаров", Value = rows.Count.ToString() },
            new ReportMetricCard { Label = "Выручка", Value = FormatMoney(totalRevenue) },
            new ReportMetricCard { Label = "Себестоимость", Value = FormatMoney(totalCost) },
            new ReportMetricCard { Label = "Валовая прибыль", Value = FormatMoney(totalProfit) },
            new ReportMetricCard { Label = "Средняя маржа", Value = $"{marginPercent:0.##} %" }
        ]);

        var categoryCaption = SelectedCategory?.Id > 0 ? SelectedCategory.Name : "Все категории";
        FilterSummary = $"Период: {ReportStartDate:dd.MM.yyyy} - {ReportEndDate:dd.MM.yyyy}. Категория: {categoryCaption}.";
        StatusText = rows.Count == 0
            ? "По выбранным параметрам данные по прибыльности не найдены."
            : $"Строк в отчёте: {rows.Count}.";
    }

    private void GenerateLossesReport()
    {
        var lossRows = _shellViewModel.GetManagementLossAnalyticsReport(ReportStartDate, ReportEndDate);
        var returnRows = _shellViewModel.GetManagementCustomerReturnAnalyticsReport(ReportStartDate, ReportEndDate);

        SalesDailyRows.Clear();
        TopProductRows.Clear();
        ProfitabilityRows.Clear();
        ReplaceCollection(LossRows, lossRows);
        ReplaceCollection(CustomerReturnRows, returnRows);

        var lossQuantity = lossRows.Sum(row => row.QuantityValue);
        var expiredQuantity = lossRows.Where(row => row.IsExpiredLoss).Sum(row => row.QuantityValue);
        var lossCost = lossRows.Sum(row => row.EstimatedCostValue);
        var returnQuantity = returnRows.Sum(row => row.QuantityValue);
        var returnAmount = returnRows.Sum(row => row.AmountValue);

        ReplaceCollection(MetricCards,
        [
            new ReportMetricCard { Label = "Списано", Value = lossQuantity.ToString() },
            new ReportMetricCard { Label = "Просрочено", Value = expiredQuantity.ToString() },
            new ReportMetricCard { Label = "Потери по себестоимости", Value = FormatMoney(lossCost) },
            new ReportMetricCard { Label = "Возвращено", Value = returnQuantity.ToString() },
            new ReportMetricCard { Label = "Сумма возвратов", Value = FormatMoney(returnAmount) }
        ]);

        FilterSummary = $"Период: {ReportStartDate:dd.MM.yyyy} - {ReportEndDate:dd.MM.yyyy}. Списания и возвраты покупателей.";
        StatusText = lossRows.Count == 0 && returnRows.Count == 0
            ? "По выбранному периоду списания и возвраты не найдены."
            : $"Строк по списаниям: {lossRows.Count}. Строк по возвратам: {returnRows.Count}.";
    }

    private void LoadLookups()
    {
        if (!ShowCategoryFilter)
        {
            return;
        }

        ReplaceCollection(CategoryOptions,
        [
            new LookupItem { Id = 0, Name = "Все категории" },
            .. _shellViewModel.GetManagementCategoryLookupItems()
        ]);

        SelectedCategory = CategoryOptions.FirstOrDefault();
    }

    private string GetFileName()
    {
        var slug = _reportCard.Key switch
        {
            ManagementReportKeys.Sales => "management-sales",
            ManagementReportKeys.Profitability => "management-profitability",
            ManagementReportKeys.Losses => "management-losses",
            _ => "management-report"
        };

        return $"{slug}_{ReportStartDate:yyyyMMdd}_{ReportEndDate:yyyyMMdd}.csv";
    }

    private string BuildCsvContent()
    {
        var builder = new StringBuilder();
        AppendCsvRow(builder, "Отчёт", _reportCard.Title);
        AppendCsvRow(builder, "Период с", ReportStartDate.ToString("dd.MM.yyyy"));
        AppendCsvRow(builder, "Период по", ReportEndDate.ToString("dd.MM.yyyy"));
        AppendManagementMetadataRows(builder);
        AppendCsvRow(builder, "Сформирован", DateTime.Now.ToString("dd.MM.yyyy HH:mm"));
        builder.AppendLine();

        switch (_reportCard.Key)
        {
            case ManagementReportKeys.Sales:
                AppendCsvRow(builder, "Динамика продаж по дням");
                AppendCsvRow(builder, "Дата", "Заказов", "Позиций", "Продано", "Выручка", "Средний чек");
                foreach (var row in SalesDailyRows)
                {
                    AppendCsvRow(builder,
                        row.ReportDateValue.ToString("dd.MM.yyyy"),
                        row.OrdersCountValue.ToString(),
                        row.PositionsCountValue.ToString(),
                        row.QuantityValue.ToString(),
                        FormatDecimal(row.RevenueValue),
                        FormatDecimal(row.AverageCheckValue));
                }

                builder.AppendLine();
                AppendCsvRow(builder, "Лидеры по товарам");
                AppendCsvRow(builder, "Товар", "Категория", "Заказов", "Продано", "Выручка");
                foreach (var row in TopProductRows)
                {
                    AppendCsvRow(builder,
                        row.ProductName,
                        row.CategoryName,
                        row.OrdersCountValue.ToString(),
                        row.QuantityValue.ToString(),
                        FormatDecimal(row.RevenueValue));
                }

                break;
            case ManagementReportKeys.Profitability:
                AppendCsvRow(builder, "Товар", "Категория", "Продано", "Выручка", "Ср. себестоимость", "Себестоимость", "Валовая прибыль", "Маржа %");
                foreach (var row in ProfitabilityRows)
                {
                    AppendCsvRow(builder,
                        row.ProductName,
                        row.CategoryName,
                        row.QuantitySoldValue.ToString(),
                        FormatDecimal(row.RevenueValue),
                        FormatDecimal(row.AveragePurchaseCostValue),
                        FormatDecimal(row.EstimatedCostValue),
                        FormatDecimal(row.GrossProfitValue),
                        row.MarginPercentValue.ToString("0.##"));
                }

                break;
            case ManagementReportKeys.Losses:
                AppendCsvRow(builder, "Списания");
                AppendCsvRow(builder, "Товар", "Категория", "Причина", "Операций", "Количество", "Потери по себестоимости");
                foreach (var row in LossRows)
                {
                    AppendCsvRow(builder,
                        row.ProductName,
                        row.CategoryName,
                        row.Reason,
                        row.OperationsCountValue.ToString(),
                        row.QuantityValue.ToString(),
                        FormatDecimal(row.EstimatedCostValue));
                }

                builder.AppendLine();
                AppendCsvRow(builder, "Возвраты покупателей");
                AppendCsvRow(builder, "Товар", "Категория", "Возвратов", "Количество", "Сумма возвратов");
                foreach (var row in CustomerReturnRows)
                {
                    AppendCsvRow(builder,
                        row.ProductName,
                        row.CategoryName,
                        row.ReturnsCountValue.ToString(),
                        row.QuantityValue.ToString(),
                        FormatDecimal(row.AmountValue));
                }

                break;
        }

        return builder.ToString();
    }

    private void AppendManagementMetadataRows(StringBuilder builder)
    {
        switch (_reportCard.Key)
        {
            case ManagementReportKeys.Sales:
                AppendCsvRow(builder, "Состав отчёта", "Продажи по дням и топ-10 товаров по выручке");
                break;
            case ManagementReportKeys.Profitability:
                AppendCsvRow(builder, "Категория", SelectedCategory?.Name ?? "Все категории");
                break;
            case ManagementReportKeys.Losses:
                AppendCsvRow(builder, "Состав отчёта", "Списания и возвраты покупателей");
                break;
        }
    }

    private bool SetProperty<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value))
        {
            return false;
        }

        field = value;
        OnPropertyChanged(propertyName);
        return true;
    }

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    private static void ReplaceCollection<T>(ObservableCollection<T> target, IEnumerable<T> items)
    {
        target.Clear();
        foreach (var item in items)
        {
            target.Add(item);
        }
    }

    private static string FormatMoney(decimal value) => $"{value:0.##} ₽";

    private static string FormatDecimal(decimal value) => value.ToString("0.##");

    private static void AppendCsvRow(StringBuilder builder, params string[] values)
    {
        builder.AppendLine(string.Join(";", values.Select(EscapeCsvValue)));
    }

    private static string EscapeCsvValue(string value)
    {
        var normalized = value.Replace("\"", "\"\"");
        return normalized.IndexOfAny([';', '"', '\r', '\n']) >= 0
            ? $"\"{normalized}\""
            : normalized;
    }
}

using System.Collections.ObjectModel;
using Applaio.Application.Imports;
using Applaio.Application.Recruitments;
using Applaio.Desktop.Dialogs;
using Applaio.Domain.Recruitments;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Windows.Storage.Pickers;
using WinRT.Interop;

namespace Applaio.Desktop;

public sealed partial class MainWindow : Window
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ObservableCollection<RecruitmentListItem> _recruitments = [];
    private List<RecruitmentListItem> _allRecruitments = [];

    public MainWindow(IServiceScopeFactory scopeFactory)
    {
        _scopeFactory = scopeFactory;

        InitializeComponent();
        ConfigureWindow();
        ConfigureTitleBar();

        RecruitmentsList.ItemsSource = _recruitments;
        _ = LoadRecruitmentsAsync();
    }

    private void ConfigureWindow()
    {
        var hwnd = WindowNative.GetWindowHandle(this);
        var windowId = Win32Interop.GetWindowIdFromWindow(hwnd);
        var appWindow = AppWindow.GetFromWindowId(windowId);

        appWindow.Resize(new Windows.Graphics.SizeInt32(1380, 860));
        appWindow.Title = "Applaio";
    }

    private void ConfigureTitleBar()
    {
        ExtendsContentIntoTitleBar = true;
        SetTitleBar(AppTitleBar);

        AppWindow.TitleBar.ButtonBackgroundColor = Colors.Transparent;
        AppWindow.TitleBar.ButtonInactiveBackgroundColor = Colors.Transparent;
    }

    private async Task LoadRecruitmentsAsync()
    {
        await using var scope = _scopeFactory.CreateAsyncScope();
        var repository = scope.ServiceProvider.GetRequiredService<IRecruitmentRepository>();
        var recruitments = await repository.GetAllAsync();

        _allRecruitments = recruitments.Select(ToListItem).ToList();
        ApplyRecruitmentFilter();
        UpdateDashboard(recruitments);
    }

    private void UpdateDashboard(IReadOnlyList<Recruitment> recruitments)
    {
        DashboardCountText.Text = recruitments.Count.ToString();
        DashboardActiveText.Text = recruitments.Count(r => !r.Status.IsClosed()).ToString();
        DashboardInterviewText.Text = recruitments.Count(r =>
            r.Status is RecruitmentStatus.HrScreening
                or RecruitmentStatus.TechnicalInterview
                or RecruitmentStatus.FinalInterview).ToString();
        DashboardOfferText.Text = recruitments.Count(r => r.Status == RecruitmentStatus.Offer).ToString();
    }

    private static RecruitmentListItem ToListItem(Recruitment recruitment)
    {
        var rate = recruitment.RateMin is null && recruitment.RateMax is null
            ? null
            : recruitment.RateMin == recruitment.RateMax
                ? $"{recruitment.RateMin:0.##}"
                : $"{recruitment.RateMin:0.##}–{recruitment.RateMax:0.##}";

        var workAndRate = string.Join(
            " • ",
            new[]
            {
                recruitment.WorkModel,
                rate is null
                    ? null
                    : string.IsNullOrWhiteSpace(recruitment.RateType)
                        ? rate
                        : $"{rate} {recruitment.RateType}"
            }.Where(value => !string.IsNullOrWhiteSpace(value)));

        var today = DateOnly.FromDateTime(DateTime.Today);
        var due = recruitment.NextActionDueOn is null
            ? "Brak terminu"
            : $"Termin: {recruitment.NextActionDueOn:dd.MM.yyyy}";

        return new RecruitmentListItem(
            recruitment.Id,
            recruitment.Company,
            recruitment.Position,
            StatusLabel(recruitment.Status),
            recruitment.Source ?? "—",
            string.IsNullOrWhiteSpace(workAndRate) ? "—" : workAndRate,
            recruitment.StartedOn.ToString("dd.MM.yyyy"),
            recruitment.PrimaryStack ?? "—",
            $"{PriorityLabel(recruitment.Priority)} • {FitLabel(recruitment.Fit)}",
            recruitment.NextAction ?? "Brak zaplanowanego kroku",
            due,
            $"{recruitment.DaysInProcess(today)} dni");
    }

    private void ApplyRecruitmentFilter()
    {
        var query = RecruitmentSearchBox?.Text?.Trim();

        IEnumerable<RecruitmentListItem> filtered = _allRecruitments;
        if (!string.IsNullOrWhiteSpace(query))
        {
            filtered = filtered.Where(item =>
                item.Company.Contains(query, StringComparison.OrdinalIgnoreCase)
                || item.Position.Contains(query, StringComparison.OrdinalIgnoreCase)
                || item.Status.Contains(query, StringComparison.OrdinalIgnoreCase)
                || item.PrimaryStack.Contains(query, StringComparison.OrdinalIgnoreCase)
                || item.Source.Contains(query, StringComparison.OrdinalIgnoreCase));
        }

        _recruitments.Clear();
        foreach (var item in filtered)
        {
            _recruitments.Add(item);
        }

        RecruitmentCountText.Text = $"{_recruitments.Count} procesów";
    }

    private async void NewRecruitmentButton_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new RecruitmentEditorDialog
        {
            XamlRoot = RootNavigation.XamlRoot
        };

        if (await dialog.ShowAsync() != ContentDialogResult.Primary)
        {
            return;
        }

        try
        {
            await using var scope = _scopeFactory.CreateAsyncScope();
            var service = scope.ServiceProvider.GetRequiredService<RecruitmentManagementService>();
            await service.CreateAsync(ToDraft(dialog.GetData()));

            await LoadRecruitmentsAsync();
            SelectNavigationItem("recruitments");
            ShowView("recruitments");
            ShowRecruitmentMessage("Dodano rekrutację", "Nowy proces został zapisany.", InfoBarSeverity.Success);
        }
        catch (Exception ex)
        {
            ShowRecruitmentMessage("Nie udało się dodać rekrutacji", ex.Message, InfoBarSeverity.Error);
        }
    }

    private async void EditRecruitmentButton_Click(object sender, RoutedEventArgs e)
    {
        if (!TryGetRecruitmentId(sender, out var id))
        {
            return;
        }

        await using var scope = _scopeFactory.CreateAsyncScope();
        var repository = scope.ServiceProvider.GetRequiredService<IRecruitmentRepository>();
        var recruitment = await repository.GetByIdAsync(id);

        if (recruitment is null)
        {
            ShowRecruitmentMessage("Nie znaleziono rekrutacji", "Proces mógł zostać wcześniej usunięty.", InfoBarSeverity.Warning);
            await LoadRecruitmentsAsync();
            return;
        }

        var dialog = new RecruitmentEditorDialog(recruitment)
        {
            XamlRoot = RootNavigation.XamlRoot
        };

        if (await dialog.ShowAsync() != ContentDialogResult.Primary)
        {
            return;
        }

        try
        {
            var service = scope.ServiceProvider.GetRequiredService<RecruitmentManagementService>();
            var updated = await service.UpdateAsync(id, ToDraft(dialog.GetData()));

            if (!updated)
            {
                ShowRecruitmentMessage("Nie zapisano zmian", "Nie znaleziono procesu do aktualizacji.", InfoBarSeverity.Warning);
                return;
            }

            await LoadRecruitmentsAsync();
            ShowRecruitmentMessage("Zapisano zmiany", "Dane i etap procesu zostały zaktualizowane.", InfoBarSeverity.Success);
        }
        catch (Exception ex)
        {
            ShowRecruitmentMessage("Nie udało się zapisać zmian", ex.Message, InfoBarSeverity.Error);
        }
    }

    private async void DeleteRecruitmentButton_Click(object sender, RoutedEventArgs e)
    {
        if (!TryGetRecruitmentId(sender, out var id))
        {
            return;
        }

        var item = _allRecruitments.FirstOrDefault(x => x.Id == id);

        var confirmation = new ContentDialog
        {
            XamlRoot = RootNavigation.XamlRoot,
            Title = "Usunąć rekrutację?",
            Content = item is null
                ? "Proces zostanie trwale usunięty z lokalnej bazy."
                : $"{item.Company} — {item.Position}\n\nProces zostanie trwale usunięty z lokalnej bazy.",
            PrimaryButtonText = "Usuń",
            CloseButtonText = "Anuluj",
            DefaultButton = ContentDialogButton.Close
        };

        if (await confirmation.ShowAsync() != ContentDialogResult.Primary)
        {
            return;
        }

        try
        {
            await using var scope = _scopeFactory.CreateAsyncScope();
            var service = scope.ServiceProvider.GetRequiredService<RecruitmentManagementService>();
            var deleted = await service.DeleteAsync(id);

            if (!deleted)
            {
                ShowRecruitmentMessage("Nie znaleziono rekrutacji", "Proces nie istnieje już w bazie.", InfoBarSeverity.Warning);
                return;
            }

            await LoadRecruitmentsAsync();
            ShowRecruitmentMessage("Usunięto rekrutację", "Proces został usunięty.", InfoBarSeverity.Success);
        }
        catch (Exception ex)
        {
            ShowRecruitmentMessage("Nie udało się usunąć rekrutacji", ex.Message, InfoBarSeverity.Error);
        }
    }

    private static RecruitmentDraft ToDraft(RecruitmentEditorData data)
        => new(
            data.Company,
            data.Position,
            data.Status,
            data.Priority,
            data.Fit,
            data.StartedOn,
            data.Source,
            data.Recruiter,
            data.OfferUrl,
            data.LastContactOn,
            data.NextAction,
            data.NextActionDueOn,
            data.WorkModel,
            data.Location,
            data.ContractType,
            data.RateMin,
            data.RateMax,
            data.RateType,
            data.PrimaryStack,
            data.KeyRequirements,
            data.Risks,
            data.Notes);

    private static bool TryGetRecruitmentId(object sender, out Guid id)
    {
        var value = (sender as Button)?.Tag?.ToString();
        return Guid.TryParse(value, out id);
    }

    private void ShowRecruitmentMessage(string title, string message, InfoBarSeverity severity)
    {
        RecruitmentResultBar.Title = title;
        RecruitmentResultBar.Message = message;
        RecruitmentResultBar.Severity = severity;
        RecruitmentResultBar.IsOpen = true;
    }

    private async void ImportWorkbookButton_Click(object sender, RoutedEventArgs e)
    {
        var picker = new FileOpenPicker();
        picker.FileTypeFilter.Add(".xlsx");

        var hwnd = WindowNative.GetWindowHandle(this);
        InitializeWithWindow.Initialize(picker, hwnd);

        var file = await picker.PickSingleFileAsync();
        if (file is null)
        {
            return;
        }

        ImportWorkbookButton.IsEnabled = false;
        ImportProgressRing.Visibility = Visibility.Visible;
        ImportProgressRing.IsActive = true;
        ImportResultBar.IsOpen = false;

        try
        {
            await using var stream = await file.OpenStreamForReadAsync();
            await using var scope = _scopeFactory.CreateAsyncScope();
            var importer = scope.ServiceProvider.GetRequiredService<RecruitmentImportService>();
            var result = await importer.ImportAsync(stream);

            ImportResultBar.Severity = result.Failed > 0
                ? InfoBarSeverity.Warning
                : InfoBarSeverity.Success;
            ImportResultBar.Title = result.Failed > 0 ? "Import częściowo zakończony" : "Import zakończony";
            ImportResultBar.Message = result.Failed > 0
                ? $"Zaimportowano: {result.Imported}, pominięto: {result.Skipped}, błędy: {result.Failed}."
                : $"Zaimportowano {result.Imported} procesów. Pominięto {result.Skipped} istniejących.";
            ImportResultBar.IsOpen = true;

            await LoadRecruitmentsAsync();

            if (result.Imported > 0 || result.Skipped > 0)
            {
                SelectNavigationItem("recruitments");
                ShowView("recruitments");
            }
        }
        catch (Exception ex)
        {
            ImportResultBar.Severity = InfoBarSeverity.Error;
            ImportResultBar.Title = "Nie udało się zaimportować pliku";
            ImportResultBar.Message = ex.Message;
            ImportResultBar.IsOpen = true;
        }
        finally
        {
            ImportProgressRing.IsActive = false;
            ImportProgressRing.Visibility = Visibility.Collapsed;
            ImportWorkbookButton.IsEnabled = true;
        }
    }

    private void RecruitmentSearchBox_TextChanged(object sender, TextChangedEventArgs e)
        => ApplyRecruitmentFilter();

    private void RootNavigation_SelectionChanged(
        NavigationView sender,
        NavigationViewSelectionChangedEventArgs args)
    {
        if (args.IsSettingsSelected)
        {
            PageTitle.Text = "Ustawienia";
            PageSubtitle.Text = "Konfiguracja Applaio.";
            ShowView("settings");
            return;
        }

        if (args.SelectedItemContainer is not NavigationViewItem item)
        {
            return;
        }

        var tag = item.Tag?.ToString() ?? "dashboard";

        (PageTitle.Text, PageSubtitle.Text) = tag switch
        {
            "recruitments" => ("Rekrutacje", "Dodawaj, edytuj i prowadź procesy od aplikacji do oferty."),
            "companies" => ("Firmy", "Firmy, do których aplikujesz lub z którymi rozmawiasz."),
            "import" => ("Import", "Jednorazowa migracja danych z Excela do lokalnej bazy."),
            _ => ("Dashboard", "Śledź cały proces rekrutacji w jednym miejscu.")
        };

        ShowView(tag);
    }

    private void SelectNavigationItem(string tag)
    {
        var item = RootNavigation.MenuItems
            .OfType<NavigationViewItem>()
            .FirstOrDefault(menuItem => string.Equals(menuItem.Tag?.ToString(), tag, StringComparison.OrdinalIgnoreCase));

        if (item is not null)
        {
            RootNavigation.SelectedItem = item;
        }
    }

    private void ShowView(string tag)
    {
        DashboardView.Visibility = tag == "dashboard" ? Visibility.Visible : Visibility.Collapsed;
        RecruitmentsView.Visibility = tag == "recruitments" ? Visibility.Visible : Visibility.Collapsed;
        CompaniesView.Visibility = tag == "companies" ? Visibility.Visible : Visibility.Collapsed;
        ImportView.Visibility = tag == "import" ? Visibility.Visible : Visibility.Collapsed;
        SettingsView.Visibility = tag == "settings" ? Visibility.Visible : Visibility.Collapsed;
    }

    private static string StatusLabel(RecruitmentStatus status) => status switch
    {
        RecruitmentStatus.ToReview => "Do przejrzenia",
        RecruitmentStatus.RecruiterContact => "Kontakt od rekrutera",
        RecruitmentStatus.Replied => "Odpowiedziano",
        RecruitmentStatus.Applied => "Aplikacja wysłana",
        RecruitmentStatus.HrScreening => "Screening HR",
        RecruitmentStatus.TechnicalInterview => "Rozmowa techniczna",
        RecruitmentStatus.RecruitmentTask => "Zadanie rekrutacyjne",
        RecruitmentStatus.NextStage => "Kolejny etap",
        RecruitmentStatus.FinalInterview => "Final interview",
        RecruitmentStatus.Offer => "Oferta",
        RecruitmentStatus.Paused => "Wstrzymana",
        RecruitmentStatus.Ghosted => "Ghosted",
        RecruitmentStatus.Rejected => "Odrzucona",
        RecruitmentStatus.Withdrawn => "Wycofana",
        RecruitmentStatus.Accepted => "Przyjęta",
        _ => status.ToString()
    };

    private static string PriorityLabel(RecruitmentPriority priority) => priority switch
    {
        RecruitmentPriority.Low => "Niski",
        RecruitmentPriority.Medium => "Średni",
        RecruitmentPriority.High => "Wysoki",
        _ => priority.ToString()
    };

    private static string FitLabel(RecruitmentFit fit) => fit switch
    {
        RecruitmentFit.Low => "Niskie",
        RecruitmentFit.Medium => "Średnie",
        RecruitmentFit.High => "Wysokie",
        RecruitmentFit.VeryHigh => "Bardzo wysokie",
        _ => fit.ToString()
    };

    private sealed record RecruitmentListItem(
        Guid Id,
        string Company,
        string Position,
        string Status,
        string Source,
        string WorkAndRate,
        string StartedOn,
        string PrimaryStack,
        string PriorityAndFit,
        string NextAction,
        string NextActionDue,
        string DaysInProcess);
}

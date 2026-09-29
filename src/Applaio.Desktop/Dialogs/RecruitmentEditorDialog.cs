using Applaio.Domain.Recruitments;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace Applaio.Desktop.Dialogs;

public sealed class RecruitmentEditorDialog : ContentDialog
{
    private readonly TextBox _company = Field("Firma");
    private readonly TextBox _position = Field("Stanowisko");
    private readonly ComboBox _status = Combo();
    private readonly ComboBox _priority = Combo();
    private readonly ComboBox _fit = Combo();
    private readonly TextBox _source = Field("Źródło");
    private readonly TextBox _recruiter = Field("Rekruter");
    private readonly TextBox _offerUrl = Field("https://...");
    private readonly CalendarDatePicker _startedOn = DateField("Data kontaktu / aplikacji");
    private readonly CalendarDatePicker _lastContactOn = DateField("Data ostatniego kontaktu");
    private readonly TextBox _nextAction = Field("Następny krok");
    private readonly CalendarDatePicker _nextActionDueOn = DateField("Termin następnego kroku");
    private readonly TextBox _workModel = Field("Zdalnie / hybrydowo / biuro");
    private readonly TextBox _location = Field("Lokalizacja");
    private readonly TextBox _contractType = Field("B2B / UoP / ...");
    private readonly NumberBox _rateMin = NumberField();
    private readonly NumberBox _rateMax = NumberField();
    private readonly TextBox _rateType = Field("np. PLN/h netto + VAT");
    private readonly TextBox _primaryStack = Field("np. .NET, Angular, Python");
    private readonly TextBox _requirements = MultilineField("Najważniejsze wymagania");
    private readonly TextBox _risks = MultilineField("Braki / ryzyka");
    private readonly TextBox _notes = MultilineField("Notatki");
    private readonly InfoBar _validation = new()
    {
        IsOpen = false,
        IsClosable = false,
        Severity = InfoBarSeverity.Error
    };

    public RecruitmentEditorDialog(Recruitment? recruitment = null)
    {
        Title = recruitment is null ? "Nowa rekrutacja" : "Edytuj rekrutację";
        PrimaryButtonText = recruitment is null ? "Dodaj" : "Zapisz";
        CloseButtonText = "Anuluj";
        DefaultButton = ContentDialogButton.Primary;

        PopulateOptions();
        Content = BuildContent();

        if (recruitment is null)
        {
            _status.SelectedIndex = 3;
            _priority.SelectedIndex = 1;
            _fit.SelectedIndex = 1;
            _startedOn.Date = DateTimeOffset.Now;
            _lastContactOn.Date = DateTimeOffset.Now;
        }
        else
        {
            Load(recruitment);
        }

        PrimaryButtonClick += ValidateBeforeClose;
    }

    public RecruitmentEditorData GetData()
    {
        var status = ((StatusOption)_status.SelectedItem).Value;
        var priority = ((PriorityOption)_priority.SelectedItem).Value;
        var fit = ((FitOption)_fit.SelectedItem).Value;

        return new RecruitmentEditorData(
            _company.Text.Trim(),
            _position.Text.Trim(),
            status,
            priority,
            fit,
            ToDateOnly(_startedOn.Date)!.Value,
            TextOrNull(_source.Text),
            TextOrNull(_recruiter.Text),
            TextOrNull(_offerUrl.Text),
            ToDateOnly(_lastContactOn.Date),
            TextOrNull(_nextAction.Text),
            ToDateOnly(_nextActionDueOn.Date),
            TextOrNull(_workModel.Text),
            TextOrNull(_location.Text),
            TextOrNull(_contractType.Text),
            NumberOrNull(_rateMin.Value),
            NumberOrNull(_rateMax.Value),
            TextOrNull(_rateType.Text),
            TextOrNull(_primaryStack.Text),
            TextOrNull(_requirements.Text),
            TextOrNull(_risks.Text),
            TextOrNull(_notes.Text));
    }

    private UIElement BuildContent()
    {
        var root = new StackPanel
        {
            Spacing = 14,
            Width = 720
        };

        root.Children.Add(_validation);

        AddSection(root, "Proces", [
            Pair("Firma", _company, "Stanowisko", _position),
            Pair("Status", _status, "Priorytet", _priority),
            Pair("Dopasowanie", _fit, "Źródło", _source),
            Pair("Rekruter", _recruiter, "Link do oferty", _offerUrl)
        ]);

        AddSection(root, "Daty i kolejny krok", [
            Pair("Start procesu", _startedOn, "Ostatni kontakt", _lastContactOn),
            Pair("Następny krok", _nextAction, "Termin", _nextActionDueOn)
        ]);

        AddSection(root, "Warunki", [
            Pair("Model pracy", _workModel, "Lokalizacja", _location),
            Pair("Forma współpracy", _contractType, "Typ stawki", _rateType),
            Pair("Stawka od", _rateMin, "Stawka do", _rateMax)
        ]);

        AddSection(root, "Dopasowanie techniczne", [
            Single("Główny stack / język", _primaryStack),
            Single("Najważniejsze wymagania", _requirements),
            Single("Braki / ryzyka", _risks),
            Single("Notatki", _notes)
        ]);

        return new ScrollViewer
        {
            Content = root,
            MaxHeight = 650
        };
    }

    private static void AddSection(StackPanel root, string title, IEnumerable<UIElement> rows)
    {
        root.Children.Add(new TextBlock
        {
            Text = title,
            FontSize = 18,
            Margin = new Thickness(0, 8, 0, 0)
        });

        foreach (var row in rows)
        {
            root.Children.Add(row);
        }
    }

    private static Grid Pair(
        string leftLabel,
        Control left,
        string rightLabel,
        Control right)
    {
        var grid = new Grid
        {
            ColumnSpacing = 14
        };
        grid.ColumnDefinitions.Add(new ColumnDefinition());
        grid.ColumnDefinitions.Add(new ColumnDefinition());

        var leftStack = Labeled(leftLabel, left);
        var rightStack = Labeled(rightLabel, right);

        Grid.SetColumn(rightStack, 1);
        grid.Children.Add(leftStack);
        grid.Children.Add(rightStack);

        return grid;
    }

    private static StackPanel Single(string label, Control control)
        => Labeled(label, control);

    private static StackPanel Labeled(string label, Control control)
    {
        var stack = new StackPanel { Spacing = 6 };
        stack.Children.Add(new TextBlock
        {
            Text = label,
            Opacity = 0.72,
            FontSize = 12
        });
        stack.Children.Add(control);
        return stack;
    }

    private void PopulateOptions()
    {
        _status.ItemsSource = new[]
        {
            new StatusOption(RecruitmentStatus.ToReview, "Do przejrzenia"),
            new StatusOption(RecruitmentStatus.RecruiterContact, "Kontakt od rekrutera"),
            new StatusOption(RecruitmentStatus.Replied, "Odpowiedziano"),
            new StatusOption(RecruitmentStatus.Applied, "Aplikacja wysłana"),
            new StatusOption(RecruitmentStatus.HrScreening, "Screening HR"),
            new StatusOption(RecruitmentStatus.TechnicalInterview, "Rozmowa techniczna"),
            new StatusOption(RecruitmentStatus.RecruitmentTask, "Zadanie rekrutacyjne"),
            new StatusOption(RecruitmentStatus.NextStage, "Kolejny etap"),
            new StatusOption(RecruitmentStatus.FinalInterview, "Final interview"),
            new StatusOption(RecruitmentStatus.Offer, "Oferta"),
            new StatusOption(RecruitmentStatus.Paused, "Wstrzymana"),
            new StatusOption(RecruitmentStatus.Ghosted, "Ghosted"),
            new StatusOption(RecruitmentStatus.Rejected, "Odrzucona"),
            new StatusOption(RecruitmentStatus.Withdrawn, "Wycofana"),
            new StatusOption(RecruitmentStatus.Accepted, "Przyjęta")
        };
        _status.DisplayMemberPath = nameof(StatusOption.Label);

        _priority.ItemsSource = new[]
        {
            new PriorityOption(RecruitmentPriority.Low, "Niski"),
            new PriorityOption(RecruitmentPriority.Medium, "Średni"),
            new PriorityOption(RecruitmentPriority.High, "Wysoki")
        };
        _priority.DisplayMemberPath = nameof(PriorityOption.Label);

        _fit.ItemsSource = new[]
        {
            new FitOption(RecruitmentFit.Low, "Niskie"),
            new FitOption(RecruitmentFit.Medium, "Średnie"),
            new FitOption(RecruitmentFit.High, "Wysokie"),
            new FitOption(RecruitmentFit.VeryHigh, "Bardzo wysokie")
        };
        _fit.DisplayMemberPath = nameof(FitOption.Label);
    }

    private void Load(Recruitment recruitment)
    {
        _company.Text = recruitment.Company;
        _position.Text = recruitment.Position;
        SelectStatus(recruitment.Status);
        SelectPriority(recruitment.Priority);
        SelectFit(recruitment.Fit);
        _source.Text = recruitment.Source ?? string.Empty;
        _recruiter.Text = recruitment.Recruiter ?? string.Empty;
        _offerUrl.Text = recruitment.OfferUrl?.ToString() ?? string.Empty;
        _startedOn.Date = ToDateTimeOffset(recruitment.StartedOn);
        _lastContactOn.Date = ToDateTimeOffset(recruitment.LastContactOn);
        _nextAction.Text = recruitment.NextAction ?? string.Empty;
        _nextActionDueOn.Date = ToDateTimeOffset(recruitment.NextActionDueOn);
        _workModel.Text = recruitment.WorkModel ?? string.Empty;
        _location.Text = recruitment.Location ?? string.Empty;
        _contractType.Text = recruitment.ContractType ?? string.Empty;
        _rateMin.Value = recruitment.RateMin is null ? double.NaN : (double)recruitment.RateMin.Value;
        _rateMax.Value = recruitment.RateMax is null ? double.NaN : (double)recruitment.RateMax.Value;
        _rateType.Text = recruitment.RateType ?? string.Empty;
        _primaryStack.Text = recruitment.PrimaryStack ?? string.Empty;
        _requirements.Text = recruitment.KeyRequirements ?? string.Empty;
        _risks.Text = recruitment.Risks ?? string.Empty;
        _notes.Text = recruitment.Notes ?? string.Empty;
    }

    private void ValidateBeforeClose(ContentDialog sender, ContentDialogButtonClickEventArgs args)
    {
        var error = Validate();
        if (error is null)
        {
            _validation.IsOpen = false;
            return;
        }

        args.Cancel = true;
        _validation.Title = "Sprawdź dane";
        _validation.Message = error;
        _validation.IsOpen = true;
    }

    private string? Validate()
    {
        if (string.IsNullOrWhiteSpace(_company.Text))
        {
            return "Firma jest wymagana.";
        }

        if (string.IsNullOrWhiteSpace(_position.Text))
        {
            return "Stanowisko jest wymagane.";
        }

        if (_status.SelectedItem is null || _priority.SelectedItem is null || _fit.SelectedItem is null)
        {
            return "Wybierz status, priorytet i dopasowanie.";
        }

        if (_startedOn.Date is null)
        {
            return "Data rozpoczęcia procesu jest wymagana.";
        }

        if (!string.IsNullOrWhiteSpace(_offerUrl.Text)
            && !Uri.TryCreate(_offerUrl.Text.Trim(), UriKind.Absolute, out _))
        {
            return "Link do oferty nie jest prawidłowym adresem URL.";
        }

        var min = NumberOrNull(_rateMin.Value);
        var max = NumberOrNull(_rateMax.Value);

        if (min is not null && max is not null && min > max)
        {
            return "Stawka od nie może być większa niż stawka do.";
        }

        return null;
    }

    private static TextBox Field(string placeholder)
        => new() { PlaceholderText = placeholder };

    private static TextBox MultilineField(string placeholder)
        => new()
        {
            PlaceholderText = placeholder,
            AcceptsReturn = true,
            TextWrapping = TextWrapping.Wrap,
            MinHeight = 74
        };

    private static ComboBox Combo()
        => new() { HorizontalAlignment = HorizontalAlignment.Stretch };

    private static CalendarDatePicker DateField(string placeholder)
        => new()
        {
            PlaceholderText = placeholder,
            HorizontalAlignment = HorizontalAlignment.Stretch
        };

    private static NumberBox NumberField()
        => new()
        {
            SpinButtonPlacementMode = NumberBoxSpinButtonPlacementMode.Compact,
            Value = double.NaN
        };

    private static string? TextOrNull(string? value)
        => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static decimal? NumberOrNull(double value)
        => double.IsNaN(value) ? null : Convert.ToDecimal(value);

    private static DateOnly? ToDateOnly(DateTimeOffset? value)
        => value is null ? null : DateOnly.FromDateTime(value.Value.DateTime);

    private static DateTimeOffset? ToDateTimeOffset(DateOnly? value)
        => value is null
            ? null
            : new DateTimeOffset(value.Value.ToDateTime(TimeOnly.MinValue));

    private void SelectStatus(RecruitmentStatus value)
        => _status.SelectedItem = _status.ItemsSource
            .Cast<StatusOption>()
            .First(option => option.Value == value);

    private void SelectPriority(RecruitmentPriority value)
        => _priority.SelectedItem = _priority.ItemsSource
            .Cast<PriorityOption>()
            .First(option => option.Value == value);

    private void SelectFit(RecruitmentFit value)
        => _fit.SelectedItem = _fit.ItemsSource
            .Cast<FitOption>()
            .First(option => option.Value == value);

    private sealed record StatusOption(RecruitmentStatus Value, string Label);
    private sealed record PriorityOption(RecruitmentPriority Value, string Label);
    private sealed record FitOption(RecruitmentFit Value, string Label);
}

public sealed record RecruitmentEditorData(
    string Company,
    string Position,
    RecruitmentStatus Status,
    RecruitmentPriority Priority,
    RecruitmentFit Fit,
    DateOnly StartedOn,
    string? Source,
    string? Recruiter,
    string? OfferUrl,
    DateOnly? LastContactOn,
    string? NextAction,
    DateOnly? NextActionDueOn,
    string? WorkModel,
    string? Location,
    string? ContractType,
    decimal? RateMin,
    decimal? RateMax,
    string? RateType,
    string? PrimaryStack,
    string? KeyRequirements,
    string? Risks,
    string? Notes);

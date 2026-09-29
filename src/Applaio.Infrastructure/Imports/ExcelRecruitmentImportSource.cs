using Applaio.Application.Imports;
using ClosedXML.Excel;

namespace Applaio.Infrastructure.Imports;

public sealed class ExcelRecruitmentImportSource : IRecruitmentImportSource
{
    private const string SheetName = "REKRUTACJE";

    private static readonly string[] RequiredHeaders =
    [
        "Firma",
        "Stanowisko",
        "Status",
        "Priorytet",
        "Dopasowanie",
        "Źródło",
        "Rekruter",
        "Link do oferty",
        "Data kontaktu / aplikacji",
        "Data ostatniego kontaktu",
        "Następny krok",
        "Termin następnego kroku",
        "Model pracy",
        "Lokalizacja",
        "Forma współpracy",
        "Stawka od",
        "Stawka do",
        "Typ stawki",
        "Główny język",
        "Najważniejsze wymagania",
        "Braki / ryzyka",
        "Notatki"
    ];

    public Task<RecruitmentImportBatch> ReadAsync(
        Stream source,
        CancellationToken cancellationToken = default)
    {
        using var workbook = new XLWorkbook(source);

        if (!workbook.TryGetWorksheet(SheetName, out var worksheet))
        {
            return Task.FromResult(new RecruitmentImportBatch(
                [],
                [new RecruitmentImportProblem(
                    null,
                    SheetName,
                    $"Brak arkusza '{SheetName}'.",
                    ImportProblemSeverity.Error)]));
        }

        var headers = worksheet.Row(1)
            .CellsUsed()
            .ToDictionary(
                cell => Normalize(cell.GetString()) ?? string.Empty,
                cell => cell.Address.ColumnNumber,
                StringComparer.OrdinalIgnoreCase);

        var problems = new List<RecruitmentImportProblem>();

        foreach (var requiredHeader in RequiredHeaders)
        {
            if (!headers.ContainsKey(requiredHeader))
            {
                problems.Add(new RecruitmentImportProblem(
                    1,
                    requiredHeader,
                    $"Brak wymaganej kolumny '{requiredHeader}'.",
                    ImportProblemSeverity.Error));
            }
        }

        if (problems.Any(problem => problem.Severity == ImportProblemSeverity.Error))
        {
            return Task.FromResult(new RecruitmentImportBatch([], problems));
        }

        var items = new List<RecruitmentImportItem>();
        var lastRow = worksheet.LastRowUsed()?.RowNumber() ?? 1;

        for (var rowNumber = 2; rowNumber <= lastRow; rowNumber++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (worksheet.Row(rowNumber).IsEmpty())
            {
                continue;
            }

            try
            {
                items.Add(ReadItem(worksheet, headers, rowNumber));
            }
            catch (ImportCellException ex)
            {
                problems.Add(new RecruitmentImportProblem(
                    rowNumber,
                    ex.Field,
                    ex.Message,
                    ImportProblemSeverity.Error));
            }
            catch (Exception ex)
            {
                problems.Add(new RecruitmentImportProblem(
                    rowNumber,
                    string.Empty,
                    ex.Message,
                    ImportProblemSeverity.Error));
            }
        }

        return Task.FromResult(new RecruitmentImportBatch(items, problems));
    }

    private static RecruitmentImportItem ReadItem(
        IXLWorksheet worksheet,
        IReadOnlyDictionary<string, int> headers,
        int row)
    {
        string RequiredText(string field)
            => Normalize(Cell(field).GetString())
               ?? throw new ImportCellException(field, $"Pole '{field}' jest wymagane.");

        string? OptionalText(string field)
            => Normalize(Cell(field).GetString());

        DateOnly RequiredDate(string field)
            => ReadDate(Cell(field), field)
               ?? throw new ImportCellException(field, $"Pole '{field}' musi zawierać datę.");

        DateOnly? OptionalDate(string field)
            => ReadDate(Cell(field), field);

        decimal? OptionalDecimal(string field)
            => ReadDecimal(Cell(field), field);

        IXLCell Cell(string field) => worksheet.Cell(row, headers[field]);

        return new RecruitmentImportItem(
            row,
            RequiredText("Firma"),
            RequiredText("Stanowisko"),
            RequiredText("Status"),
            RequiredText("Priorytet"),
            RequiredText("Dopasowanie"),
            RequiredText("Źródło"),
            OptionalText("Rekruter"),
            RequiredText("Link do oferty"),
            RequiredDate("Data kontaktu / aplikacji"),
            RequiredDate("Data ostatniego kontaktu"),
            OptionalText("Następny krok"),
            OptionalDate("Termin następnego kroku"),
            RequiredText("Model pracy"),
            RequiredText("Lokalizacja"),
            RequiredText("Forma współpracy"),
            OptionalDecimal("Stawka od"),
            OptionalDecimal("Stawka do"),
            OptionalText("Typ stawki"),
            RequiredText("Główny język"),
            OptionalText("Najważniejsze wymagania"),
            OptionalText("Braki / ryzyka"),
            OptionalText("Notatki"));
    }

    private static DateOnly? ReadDate(IXLCell cell, string field)
    {
        var text = Normalize(cell.GetString());
        if (text is null)
        {
            return null;
        }

        if (cell.TryGetValue<DateTime>(out var dateTime))
        {
            return DateOnly.FromDateTime(dateTime);
        }

        if (DateOnly.TryParse(text, out var date))
        {
            return date;
        }

        throw new ImportCellException(field, $"Nieprawidłowa data: '{text}'.");
    }

    private static decimal? ReadDecimal(IXLCell cell, string field)
    {
        var text = Normalize(cell.GetString());
        if (text is null)
        {
            return null;
        }

        if (cell.TryGetValue<decimal>(out var value))
        {
            return value;
        }

        throw new ImportCellException(field, $"Nieprawidłowa wartość stawki: '{text}'.");
    }

    private static string? Normalize(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var normalized = value.Trim();

        return normalized.Equals("b/d", StringComparison.OrdinalIgnoreCase)
            || normalized.Equals("n/d", StringComparison.OrdinalIgnoreCase)
            || normalized.Equals("nie podano", StringComparison.OrdinalIgnoreCase)
                ? null
                : normalized;
    }

    private sealed class ImportCellException(string field, string message) : Exception(message)
    {
        public string Field { get; } = field;
    }
}

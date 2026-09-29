namespace Applaio.Application.Imports;

public interface IRecruitmentImportSource
{
    Task<RecruitmentImportBatch> ReadAsync(Stream source, CancellationToken cancellationToken = default);
}

public sealed record RecruitmentImportBatch(
    IReadOnlyList<RecruitmentImportRow> Rows,
    IReadOnlyList<RecruitmentImportProblem> Problems);

public sealed record RecruitmentImportRow(
    int SourceRow,
    IReadOnlyDictionary<string, string?> Values);

public sealed record RecruitmentImportProblem(
    int? SourceRow,
    string Field,
    string Message,
    ImportProblemSeverity Severity);

public enum ImportProblemSeverity
{
    Information,
    Warning,
    Error
}

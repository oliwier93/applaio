namespace Applaio.Application.Imports;

public interface IRecruitmentImportSource
{
    Task<RecruitmentImportBatch> ReadAsync(Stream source, CancellationToken cancellationToken = default);
}

public sealed record RecruitmentImportBatch(
    IReadOnlyList<RecruitmentImportItem> Items,
    IReadOnlyList<RecruitmentImportProblem> Problems);

public sealed record RecruitmentImportItem(
    int SourceRow,
    string Company,
    string Position,
    string Status,
    string Priority,
    string Fit,
    string Source,
    string? Recruiter,
    string? OfferUrl,
    DateOnly StartedOn,
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

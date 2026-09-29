using Applaio.Domain.Recruitments;

namespace Applaio.Application.Recruitments;

public sealed class RecruitmentManagementService(IRecruitmentRepository repository)
{
    public async Task<Guid> CreateAsync(
        RecruitmentDraft draft,
        CancellationToken cancellationToken = default)
    {
        var recruitment = new Recruitment(
            Guid.NewGuid(),
            draft.Company,
            draft.Position,
            draft.Status,
            draft.Priority,
            draft.Fit,
            draft.StartedOn);

        Apply(recruitment, draft);

        await repository.AddAsync(recruitment, cancellationToken);
        await repository.SaveChangesAsync(cancellationToken);

        return recruitment.Id;
    }

    public async Task<bool> UpdateAsync(
        Guid id,
        RecruitmentDraft draft,
        CancellationToken cancellationToken = default)
    {
        var recruitment = await repository.GetByIdAsync(id, cancellationToken);
        if (recruitment is null)
        {
            return false;
        }

        recruitment.UpdateIdentity(draft.Company, draft.Position, draft.StartedOn);
        recruitment.SetStatus(draft.Status);
        recruitment.SetClassification(draft.Priority, draft.Fit);
        Apply(recruitment, draft);

        await repository.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<bool> DeleteAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var recruitment = await repository.GetByIdAsync(id, cancellationToken);
        if (recruitment is null)
        {
            return false;
        }

        repository.Remove(recruitment);
        await repository.SaveChangesAsync(cancellationToken);
        return true;
    }

    private static void Apply(Recruitment recruitment, RecruitmentDraft draft)
    {
        Uri? offerUrl = null;
        if (!string.IsNullOrWhiteSpace(draft.OfferUrl))
        {
            offerUrl = new Uri(draft.OfferUrl, UriKind.Absolute);
        }

        recruitment.SetOrigin(draft.Source, draft.Recruiter, offerUrl);
        recruitment.SetContact(draft.LastContactOn, draft.NextAction, draft.NextActionDueOn);
        recruitment.SetWork(draft.WorkModel, draft.Location, draft.ContractType);
        recruitment.SetCompensation(draft.RateMin, draft.RateMax, draft.RateType);
        recruitment.SetDetails(
            draft.PrimaryStack,
            draft.KeyRequirements,
            draft.Risks,
            draft.Notes);
    }
}

public sealed record RecruitmentDraft(
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

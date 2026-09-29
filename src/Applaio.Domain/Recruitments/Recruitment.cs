namespace Applaio.Domain.Recruitments;

public sealed class Recruitment
{
    private Recruitment() { }

    public Recruitment(
        Guid id,
        string company,
        string position,
        RecruitmentStatus status,
        RecruitmentPriority priority,
        RecruitmentFit fit,
        DateOnly startedOn)
    {
        Id = id;
        Company = Require(company, nameof(company));
        Position = Require(position, nameof(position));
        Status = status;
        Priority = priority;
        Fit = fit;
        StartedOn = startedOn;
        LastContactOn = startedOn;
        CreatedAt = DateTimeOffset.UtcNow;
        UpdatedAt = CreatedAt;
    }

    public Guid Id { get; private set; }
    public string Company { get; private set; } = string.Empty;
    public string Position { get; private set; } = string.Empty;
    public RecruitmentStatus Status { get; private set; }
    public RecruitmentPriority Priority { get; private set; }
    public RecruitmentFit Fit { get; private set; }

    public string? Source { get; private set; }
    public string? Recruiter { get; private set; }
    public Uri? OfferUrl { get; private set; }

    public DateOnly StartedOn { get; private set; }
    public DateOnly? LastContactOn { get; private set; }
    public string? NextAction { get; private set; }
    public DateOnly? NextActionDueOn { get; private set; }

    public string? WorkModel { get; private set; }
    public string? Location { get; private set; }
    public string? ContractType { get; private set; }

    public decimal? RateMin { get; private set; }
    public decimal? RateMax { get; private set; }
    public string? RateType { get; private set; }

    public string? PrimaryStack { get; private set; }
    public string? KeyRequirements { get; private set; }
    public string? Risks { get; private set; }
    public string? Notes { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }

    public int DaysInProcess(DateOnly today) => Math.Max(0, today.DayNumber - StartedOn.DayNumber);

    public int? DaysSinceLastContact(DateOnly today)
        => LastContactOn is null
            ? null
            : Math.Max(0, today.DayNumber - LastContactOn.Value.DayNumber);

    public void UpdateIdentity(string company, string position, DateOnly startedOn)
    {
        Company = Require(company, nameof(company));
        Position = Require(position, nameof(position));
        StartedOn = startedOn;
        Touch();
    }

    public void SetStatus(RecruitmentStatus status)
    {
        Status = status;
        Touch();
    }

    public void SetClassification(RecruitmentPriority priority, RecruitmentFit fit)
    {
        Priority = priority;
        Fit = fit;
        Touch();
    }

    public void SetOrigin(string? source, string? recruiter, Uri? offerUrl)
    {
        Source = Normalize(source);
        Recruiter = Normalize(recruiter);
        OfferUrl = offerUrl;
        Touch();
    }

    public void SetContact(DateOnly? lastContactOn, string? nextAction, DateOnly? nextActionDueOn)
    {
        LastContactOn = lastContactOn;
        NextAction = Normalize(nextAction);
        NextActionDueOn = nextActionDueOn;
        Touch();
    }

    public void SetWork(string? workModel, string? location, string? contractType)
    {
        WorkModel = Normalize(workModel);
        Location = Normalize(location);
        ContractType = Normalize(contractType);
        Touch();
    }

    public void SetCompensation(decimal? rateMin, decimal? rateMax, string? rateType)
    {
        if (rateMin is not null && rateMax is not null && rateMin > rateMax)
        {
            throw new ArgumentException("Minimum rate cannot be greater than maximum rate.");
        }

        RateMin = rateMin;
        RateMax = rateMax;
        RateType = Normalize(rateType);
        Touch();
    }

    public void SetDetails(
        string? primaryStack,
        string? keyRequirements,
        string? risks,
        string? notes)
    {
        PrimaryStack = Normalize(primaryStack);
        KeyRequirements = Normalize(keyRequirements);
        Risks = Normalize(risks);
        Notes = Normalize(notes);
        Touch();
    }

    private void Touch() => UpdatedAt = DateTimeOffset.UtcNow;

    private static string Require(string value, string paramName)
        => string.IsNullOrWhiteSpace(value)
            ? throw new ArgumentException("Value cannot be empty.", paramName)
            : value.Trim();

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
}

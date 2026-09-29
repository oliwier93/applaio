namespace Applaio.Domain.Recruitments;

public sealed class Recruitment
{
    private Recruitment() { }

    public Recruitment(
        Guid id,
        string company,
        string position,
        RecruitmentStatus status,
        DateOnly appliedOn,
        string? source = null,
        Uri? offerUrl = null)
    {
        Id = id;
        Company = Require(company, nameof(company));
        Position = Require(position, nameof(position));
        Status = status;
        AppliedOn = appliedOn;
        Source = Normalize(source);
        OfferUrl = offerUrl;
    }

    public Guid Id { get; private set; }
    public string Company { get; private set; } = string.Empty;
    public string Position { get; private set; } = string.Empty;
    public RecruitmentStatus Status { get; private set; }
    public DateOnly AppliedOn { get; private set; }
    public string? Source { get; private set; }
    public Uri? OfferUrl { get; private set; }
    public string? ContactPerson { get; private set; }
    public string? Notes { get; private set; }
    public decimal? RateMin { get; private set; }
    public decimal? RateMax { get; private set; }
    public string? RateCurrency { get; private set; }
    public string? ContractType { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; } = DateTimeOffset.UtcNow;

    public void UpdateStatus(RecruitmentStatus status)
    {
        Status = status;
        Touch();
    }

    public void UpdateDetails(
        string? contactPerson,
        string? notes,
        decimal? rateMin,
        decimal? rateMax,
        string? rateCurrency,
        string? contractType)
    {
        ContactPerson = Normalize(contactPerson);
        Notes = Normalize(notes);
        RateMin = rateMin;
        RateMax = rateMax;
        RateCurrency = Normalize(rateCurrency);
        ContractType = Normalize(contractType);
        Touch();
    }

    private void Touch() => UpdatedAt = DateTimeOffset.UtcNow;

    private static string Require(string value, string paramName)
        => string.IsNullOrWhiteSpace(value)
            ? throw new ArgumentException("Value cannot be empty.", paramName)
            : value.Trim();

    private static string? Normalize(string? value)
        => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}

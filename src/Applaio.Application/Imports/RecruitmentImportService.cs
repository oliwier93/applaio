using Applaio.Application.Recruitments;
using Applaio.Domain.Recruitments;

namespace Applaio.Application.Imports;

public sealed class RecruitmentImportService(
    IRecruitmentImportSource importSource,
    IRecruitmentRepository repository)
{
    public async Task<RecruitmentImportResult> ImportAsync(
        Stream source,
        CancellationToken cancellationToken = default)
    {
        var batch = await importSource.ReadAsync(source, cancellationToken);

        if (batch.Problems.Any(problem => problem.Severity == ImportProblemSeverity.Error))
        {
            return new RecruitmentImportResult(0, 0, batch.Problems);
        }

        var imported = 0;
        var skipped = 0;
        var problems = batch.Problems.ToList();

        foreach (var item in batch.Items)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (await repository.ExistsAsync(
                    item.Company,
                    item.Position,
                    item.StartedOn,
                    cancellationToken))
            {
                skipped++;
                problems.Add(new RecruitmentImportProblem(
                    item.SourceRow,
                    "Firma",
                    "Proces już istnieje i został pominięty.",
                    ImportProblemSeverity.Information));
                continue;
            }

            if (!TryMap(item, out var recruitment, out var mappingProblem))
            {
                problems.Add(mappingProblem!);
                continue;
            }

            await repository.AddAsync(recruitment!, cancellationToken);
            imported++;
        }

        if (imported > 0)
        {
            await repository.SaveChangesAsync(cancellationToken);
        }

        return new RecruitmentImportResult(imported, skipped, problems);
    }

    private static bool TryMap(
        RecruitmentImportItem item,
        out Recruitment? recruitment,
        out RecruitmentImportProblem? problem)
    {
        recruitment = null;
        problem = null;

        if (!TryStatus(item.Status, out var status))
        {
            problem = Unknown(item, "Status", item.Status);
            return false;
        }

        if (!TryPriority(item.Priority, out var priority))
        {
            problem = Unknown(item, "Priorytet", item.Priority);
            return false;
        }

        if (!TryFit(item.Fit, out var fit))
        {
            problem = Unknown(item, "Dopasowanie", item.Fit);
            return false;
        }

        Uri? offerUrl = null;
        if (!string.IsNullOrWhiteSpace(item.OfferUrl)
            && !Uri.TryCreate(item.OfferUrl, UriKind.Absolute, out offerUrl))
        {
            problem = new RecruitmentImportProblem(
                item.SourceRow,
                "Link do oferty",
                $"Nieprawidłowy adres URL: '{item.OfferUrl}'.",
                ImportProblemSeverity.Error);
            return false;
        }

        recruitment = new Recruitment(
            Guid.NewGuid(),
            item.Company,
            item.Position,
            status,
            priority,
            fit,
            item.StartedOn);

        recruitment.SetOrigin(item.Source, item.Recruiter, offerUrl);
        recruitment.SetContact(item.LastContactOn, item.NextAction, item.NextActionDueOn);
        recruitment.SetWork(item.WorkModel, item.Location, item.ContractType);
        recruitment.SetCompensation(item.RateMin, item.RateMax, item.RateType);
        recruitment.SetDetails(item.PrimaryStack, item.KeyRequirements, item.Risks, item.Notes);

        return true;
    }

    private static RecruitmentImportProblem Unknown(
        RecruitmentImportItem item,
        string field,
        string value)
        => new(
            item.SourceRow,
            field,
            $"Nieznana wartość '{value}'. Dodaj mapowanie przed importem.",
            ImportProblemSeverity.Error);

    private static bool TryStatus(string value, out RecruitmentStatus status)
    {
        status = value.Trim() switch
        {
            "Do przejrzenia" => RecruitmentStatus.ToReview,
            "Kontakt od rekrutera" => RecruitmentStatus.RecruiterContact,
            "Odpowiedziano" => RecruitmentStatus.Replied,
            "Aplikacja wysłana" => RecruitmentStatus.Applied,
            "Screening HR" => RecruitmentStatus.HrScreening,
            "Rozmowa techniczna" => RecruitmentStatus.TechnicalInterview,
            "Zadanie rekrutacyjne" => RecruitmentStatus.RecruitmentTask,
            "Kolejny etap" => RecruitmentStatus.NextStage,
            "Final interview" => RecruitmentStatus.FinalInterview,
            "Oferta" => RecruitmentStatus.Offer,
            "Wstrzymana" => RecruitmentStatus.Paused,
            "Ghosted" => RecruitmentStatus.Ghosted,
            "Odrzucona" => RecruitmentStatus.Rejected,
            "Wycofana" => RecruitmentStatus.Withdrawn,
            "Przyjęta" => RecruitmentStatus.Accepted,
            _ => default
        };

        return value.Trim() is
            "Do przejrzenia" or
            "Kontakt od rekrutera" or
            "Odpowiedziano" or
            "Aplikacja wysłana" or
            "Screening HR" or
            "Rozmowa techniczna" or
            "Zadanie rekrutacyjne" or
            "Kolejny etap" or
            "Final interview" or
            "Oferta" or
            "Wstrzymana" or
            "Ghosted" or
            "Odrzucona" or
            "Wycofana" or
            "Przyjęta";
    }

    private static bool TryPriority(string value, out RecruitmentPriority priority)
    {
        priority = value.Trim() switch
        {
            "Niski" => RecruitmentPriority.Low,
            "Średni" => RecruitmentPriority.Medium,
            "Wysoki" => RecruitmentPriority.High,
            _ => default
        };

        return value.Trim() is "Niski" or "Średni" or "Wysoki";
    }

    private static bool TryFit(string value, out RecruitmentFit fit)
    {
        fit = value.Trim() switch
        {
            "Niskie" => RecruitmentFit.Low,
            "Średnie" => RecruitmentFit.Medium,
            "Wysokie" => RecruitmentFit.High,
            "Bardzo wysokie" => RecruitmentFit.VeryHigh,
            _ => default
        };

        return value.Trim() is "Niskie" or "Średnie" or "Wysokie" or "Bardzo wysokie";
    }
}

public sealed record RecruitmentImportResult(
    int Imported,
    int Skipped,
    IReadOnlyList<RecruitmentImportProblem> Problems);

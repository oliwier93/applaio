namespace Applaio.Domain.Recruitments;

public enum RecruitmentStatus
{
    Saved = 0,
    Applied = 10,
    RecruiterContact = 20,
    Screening = 30,
    TechnicalInterview = 40,
    FinalInterview = 50,
    Offer = 60,
    Accepted = 70,
    Rejected = 80,
    Withdrawn = 90,
    NoResponse = 100
}

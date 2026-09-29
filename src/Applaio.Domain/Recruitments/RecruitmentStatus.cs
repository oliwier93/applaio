namespace Applaio.Domain.Recruitments;

public enum RecruitmentStatus
{
    ToReview = 0,
    RecruiterContact = 10,
    Replied = 20,
    Applied = 30,
    HrScreening = 40,
    TechnicalInterview = 50,
    RecruitmentTask = 60,
    NextStage = 70,
    FinalInterview = 80,
    Offer = 90,
    Paused = 100,
    Ghosted = 110,
    Rejected = 120,
    Withdrawn = 130,
    Accepted = 140
}

public static class RecruitmentStatusExtensions
{
    public static bool IsClosed(this RecruitmentStatus status)
        => status is RecruitmentStatus.Rejected
            or RecruitmentStatus.Withdrawn
            or RecruitmentStatus.Accepted;

    public static bool RequiresAttention(this RecruitmentStatus status)
        => status is RecruitmentStatus.RecruiterContact
            or RecruitmentStatus.Replied
            or RecruitmentStatus.HrScreening
            or RecruitmentStatus.TechnicalInterview
            or RecruitmentStatus.RecruitmentTask
            or RecruitmentStatus.NextStage
            or RecruitmentStatus.FinalInterview
            or RecruitmentStatus.Offer;
}

using Applaio.Domain.Recruitments;

namespace Applaio.Application.Recruitments;

public interface IRecruitmentRepository
{
    Task<IReadOnlyList<Recruitment>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<Recruitment?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task AddAsync(Recruitment recruitment, CancellationToken cancellationToken = default);
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}

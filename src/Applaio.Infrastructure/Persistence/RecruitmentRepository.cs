using Applaio.Application.Recruitments;
using Applaio.Domain.Recruitments;
using Microsoft.EntityFrameworkCore;

namespace Applaio.Infrastructure.Persistence;

public sealed class RecruitmentRepository(ApplaioDbContext dbContext) : IRecruitmentRepository
{
    public async Task<IReadOnlyList<Recruitment>> GetAllAsync(CancellationToken cancellationToken = default)
        => await dbContext.Recruitments
            .AsNoTracking()
            .OrderByDescending(x => x.AppliedOn)
            .ThenBy(x => x.Company)
            .ToListAsync(cancellationToken);

    public Task<Recruitment?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        => dbContext.Recruitments.SingleOrDefaultAsync(x => x.Id == id, cancellationToken);

    public Task AddAsync(Recruitment recruitment, CancellationToken cancellationToken = default)
        => dbContext.Recruitments.AddAsync(recruitment, cancellationToken).AsTask();

    public Task SaveChangesAsync(CancellationToken cancellationToken = default)
        => dbContext.SaveChangesAsync(cancellationToken);
}

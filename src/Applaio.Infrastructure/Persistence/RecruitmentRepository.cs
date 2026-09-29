using Applaio.Application.Recruitments;
using Applaio.Domain.Recruitments;
using Microsoft.EntityFrameworkCore;

namespace Applaio.Infrastructure.Persistence;

public sealed class RecruitmentRepository(ApplaioDbContext dbContext) : IRecruitmentRepository
{
    public async Task<IReadOnlyList<Recruitment>> GetAllAsync(CancellationToken cancellationToken = default)
        => await dbContext.Recruitments
            .AsNoTracking()
            .OrderByDescending(x => x.StartedOn)
            .ThenBy(x => x.Company)
            .ToListAsync(cancellationToken);

    public Task<Recruitment?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        => dbContext.Recruitments.SingleOrDefaultAsync(x => x.Id == id, cancellationToken);

    public Task<bool> ExistsAsync(
        string company,
        string position,
        DateOnly startedOn,
        CancellationToken cancellationToken = default)
        => dbContext.Recruitments.AnyAsync(
            x => x.Company == company
                 && x.Position == position
                 && x.StartedOn == startedOn,
            cancellationToken);

    public Task AddAsync(Recruitment recruitment, CancellationToken cancellationToken = default)
        => dbContext.Recruitments.AddAsync(recruitment, cancellationToken).AsTask();

    public void Remove(Recruitment recruitment)
        => dbContext.Recruitments.Remove(recruitment);

    public Task SaveChangesAsync(CancellationToken cancellationToken = default)
        => dbContext.SaveChangesAsync(cancellationToken);
}

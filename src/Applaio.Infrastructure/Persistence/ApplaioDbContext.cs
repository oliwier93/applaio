using Applaio.Domain.Recruitments;
using Microsoft.EntityFrameworkCore;

namespace Applaio.Infrastructure.Persistence;

public sealed class ApplaioDbContext(DbContextOptions<ApplaioDbContext> options) : DbContext(options)
{
    public DbSet<Recruitment> Recruitments => Set<Recruitment>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        var recruitment = modelBuilder.Entity<Recruitment>();

        recruitment.ToTable("Recruitments");
        recruitment.HasKey(x => x.Id);

        recruitment.Property(x => x.Company).HasMaxLength(200).IsRequired();
        recruitment.Property(x => x.Position).HasMaxLength(250).IsRequired();

        recruitment.Property(x => x.Status).HasConversion<string>().HasMaxLength(64);
        recruitment.Property(x => x.Priority).HasConversion<string>().HasMaxLength(32);
        recruitment.Property(x => x.Fit).HasConversion<string>().HasMaxLength(32);

        recruitment.Property(x => x.Source).HasMaxLength(160);
        recruitment.Property(x => x.Recruiter).HasMaxLength(200);
        recruitment.Property(x => x.OfferUrl)
            .HasConversion(
                value => value == null ? null : value.ToString(),
                value => string.IsNullOrWhiteSpace(value) ? null : new Uri(value));

        recruitment.Property(x => x.NextAction).HasMaxLength(300);
        recruitment.Property(x => x.WorkModel).HasMaxLength(80);
        recruitment.Property(x => x.Location).HasMaxLength(160);
        recruitment.Property(x => x.ContractType).HasMaxLength(80);
        recruitment.Property(x => x.RateType).HasMaxLength(80);
        recruitment.Property(x => x.PrimaryStack).HasMaxLength(160);

        recruitment.Property(x => x.RateMin).HasPrecision(18, 2);
        recruitment.Property(x => x.RateMax).HasPrecision(18, 2);

        recruitment.HasIndex(x => new { x.Company, x.Position, x.StartedOn });
        recruitment.HasIndex(x => x.Status);
        recruitment.HasIndex(x => x.NextActionDueOn);
    }
}

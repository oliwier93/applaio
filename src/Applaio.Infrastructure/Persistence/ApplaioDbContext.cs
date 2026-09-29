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
        recruitment.Property(x => x.Source).HasMaxLength(120);
        recruitment.Property(x => x.ContactPerson).HasMaxLength(200);
        recruitment.Property(x => x.RateCurrency).HasMaxLength(8);
        recruitment.Property(x => x.ContractType).HasMaxLength(80);
        recruitment.Property(x => x.OfferUrl)
            .HasConversion(
                value => value == null ? null : value.ToString(),
                value => string.IsNullOrWhiteSpace(value) ? null : new Uri(value));
        recruitment.Property(x => x.Status).HasConversion<string>().HasMaxLength(64);
        recruitment.Property(x => x.AppliedOn).HasConversion<string>();
        recruitment.HasIndex(x => new { x.Company, x.Position, x.AppliedOn });
    }
}

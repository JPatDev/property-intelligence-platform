using Microsoft.EntityFrameworkCore;
using PropertyIntelligence.Modules.Documents.Domain;

namespace PropertyIntelligence.Modules.Documents.Infrastructure.Persistence;

public sealed class DocumentsDbContext(DbContextOptions<DocumentsDbContext> options) : DbContext(options)
{
    public DbSet<DocumentRecord> Documents => Set<DocumentRecord>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema(DocumentsSchema.Name);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(DocumentsDbContext).Assembly);
    }
}

internal static class DocumentsSchema
{
    public const string Name = "documents";
}

using System.Text.Json;
using CJDSL.Domain.Entities.Dsl;
using CJDSL.Domain.Entities.MetaModel;
using Microsoft.EntityFrameworkCore;

namespace CJDSL.Infrastructure.Persistence;

public class CJDSLDbContext : DbContext
{
    public DbSet<DslPageEntity> DslPages => Set<DslPageEntity>();
    public DbSet<MetaObjectEntity> MetaObjects => Set<MetaObjectEntity>();
    public DbSet<MetaEnumEntity> MetaEnums => Set<MetaEnumEntity>();
    public DbSet<MetaDictionaryEntity> MetaDictionaries => Set<MetaDictionaryEntity>();

    public CJDSLDbContext(DbContextOptions<CJDSLDbContext> options) : base(options) { }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<DslPageEntity>(e =>
        {
            e.HasKey(x => x.PageCode);
            e.Property(x => x.JsonContent).IsRequired();
            e.Property(x => x.Title).HasMaxLength(200);
            e.Property(x => x.Layout).HasMaxLength(50);
        });

        modelBuilder.Entity<MetaObjectEntity>(e =>
        {
            e.HasKey(x => x.Code);
            e.Property(x => x.JsonContent).IsRequired();
            e.Property(x => x.Name).HasMaxLength(200);
        });

        modelBuilder.Entity<MetaEnumEntity>(e =>
        {
            e.HasKey(x => x.Code);
            e.Property(x => x.JsonContent).IsRequired();
            e.Property(x => x.Name).HasMaxLength(200);
        });

        modelBuilder.Entity<MetaDictionaryEntity>(e =>
        {
            e.HasKey(x => x.Code);
            e.Property(x => x.JsonContent).IsRequired();
            e.Property(x => x.Name).HasMaxLength(200);
        });
    }
}

public class DslPageEntity
{
    public string PageCode { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Layout { get; set; } = "form";
    public string Version { get; set; } = "1.0.0";
    public string JsonContent { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

public class MetaObjectEntity
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string JsonContent { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
}

public class MetaEnumEntity
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string JsonContent { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public class MetaDictionaryEntity
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string JsonContent { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

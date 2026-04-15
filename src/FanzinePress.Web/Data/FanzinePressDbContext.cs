using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using FanzinePress.Web.Models;

namespace FanzinePress.Web.Data;

public class FanzinePressDbContext : IdentityDbContext<ApplicationUser>
{
    public FanzinePressDbContext(DbContextOptions<FanzinePressDbContext> options)
        : base(options)
    {
    }

    public DbSet<Issue> Issues => Set<Issue>();
    public DbSet<Article> Articles => Set<Article>();
    public DbSet<Photo> Photos => Set<Photo>();
    public DbSet<Ad> Ads => Set<Ad>();
    public DbSet<Colophon> Colophons => Set<Colophon>();
    public DbSet<IssueSettings> IssueSettings => Set<IssueSettings>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Issue>(e =>
        {
            e.HasMany(i => i.Articles).WithOne(a => a.Issue).HasForeignKey(a => a.IssueId).OnDelete(DeleteBehavior.Cascade);
            e.HasMany(i => i.Ads).WithOne(a => a.Issue).HasForeignKey(a => a.IssueId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(i => i.Colophon).WithOne(c => c.Issue).HasForeignKey<Colophon>(c => c.IssueId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(i => i.Settings).WithOne(s => s.Issue).HasForeignKey<IssueSettings>(s => s.IssueId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(i => i.Owner).WithMany().HasForeignKey(i => i.OwnerId).OnDelete(DeleteBehavior.SetNull);
            e.HasIndex(i => i.OwnerId);
        });

        modelBuilder.Entity<Article>(e =>
        {
            e.HasMany(a => a.Photos).WithOne(p => p.Article).HasForeignKey(p => p.ArticleId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Ad>(e =>
        {
            e.Property(a => a.Size).HasConversion<string>();
        });

        modelBuilder.Entity<Issue>(e =>
        {
            e.Property(i => i.Status).HasConversion<string>();
        });
    }
}

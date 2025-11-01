using Microsoft.EntityFrameworkCore;
using electrostore.Models;

namespace electrostore;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }
    public DbSet<electrostore.Models.Boxs> Boxs { get; set; }
    public DbSet<electrostore.Models.BoxsTags> BoxsTags { get; set; }
    public DbSet<electrostore.Models.Cameras> Cameras { get; set; }
    public DbSet<electrostore.Models.Commands> Commands { get; set; }
    public DbSet<electrostore.Models.CommandsCommentaires> CommandsCommentaires { get; set; }
    public DbSet<electrostore.Models.CommandsDocuments> CommandsDocuments { get; set; }
    public DbSet<electrostore.Models.CommandsItems> CommandsItems { get; set; }
    public DbSet<electrostore.Models.IA> IA { get; set; }
    public DbSet<electrostore.Models.Imgs> Imgs { get; set; }
    public DbSet<electrostore.Models.Items> Items { get; set; }
    public DbSet<electrostore.Models.ItemsBoxs> ItemsBoxs { get; set; }
    public DbSet<electrostore.Models.ItemsDocuments> ItemsDocuments { get; set; }
    public DbSet<electrostore.Models.ItemsTags> ItemsTags { get; set; }
    public DbSet<electrostore.Models.JwiAccessTokens> JwiAccessTokens { get; set; }
    public DbSet<electrostore.Models.JwiRefreshTokens> JwiRefreshTokens { get; set; }
    public DbSet<electrostore.Models.Leds> Leds { get; set; }
    public DbSet<electrostore.Models.Projets> Projets { get; set; }
    public DbSet<electrostore.Models.ProjetsCommentaires> ProjetsCommentaires { get; set; }
    public DbSet<electrostore.Models.ProjetsDocuments> ProjetsDocuments { get; set; }
    public DbSet<electrostore.Models.ProjetsItems> ProjetsItems { get; set; }
    public DbSet<electrostore.Models.Stores> Stores { get; set; }
    public DbSet<electrostore.Models.StoresTags> StoresTags { get; set; }
    public DbSet<electrostore.Models.Tags> Tags { get; set; }
    public DbSet<electrostore.Models.Users> Users { get; set; }

    public override int SaveChanges()
    {
        AddTimestamps();
        return base.SaveChanges();
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        AddTimestamps();
        return base.SaveChangesAsync(cancellationToken);
    }

    private void AddTimestamps()
    {
        var entities = ChangeTracker.Entries()
            .Where(x => x.Entity is BaseEntity && (x.State == EntityState.Added || x.State == EntityState.Modified));

        foreach (var entity in entities)
        {
            var now = DateTime.UtcNow;
            if (entity.State == EntityState.Added)
            {
                ((BaseEntity)entity.Entity).created_at = now;
            }
            ((BaseEntity)entity.Entity).updated_at = now;
        }
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<BoxsTags>()
            .HasKey(bt => new { bt.id_box, bt.id_tag });

        modelBuilder.Entity<CommandsItems>()
            .HasKey(ci => new { ci.id_command, ci.id_item });

        modelBuilder.Entity<ItemsBoxs>()
            .HasKey(ib => new { ib.id_item, ib.id_box });
        
        modelBuilder.Entity<ItemsTags>()
            .HasKey(it => new { it.id_item, it.id_tag });

        modelBuilder.Entity<ProjetsItems>()
            .HasKey(pi => new { pi.id_projet, pi.id_item });

        modelBuilder.Entity<StoresTags>()
            .HasKey(st => new { st.id_store, st.id_tag });
    }
}
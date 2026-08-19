using ElectrostoreAPI.Models;
using Microsoft.EntityFrameworkCore;

namespace ElectrostoreAPI;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }
    public DbSet<ElectrostoreAPI.Models.Boxs> Boxs { get; set; }
    public DbSet<ElectrostoreAPI.Models.BoxsTags> BoxsTags { get; set; }
    public DbSet<ElectrostoreAPI.Models.Cameras> Cameras { get; set; }
    public DbSet<ElectrostoreAPI.Models.Carriers> Carriers { get; set; }
    public DbSet<ElectrostoreAPI.Models.CronJobs> CronJobs { get; set; }
    public DbSet<ElectrostoreAPI.Models.Commands> Commands { get; set; }
    public DbSet<ElectrostoreAPI.Models.CommandsComments> CommandsComments { get; set; }
    public DbSet<ElectrostoreAPI.Models.CommandsDocuments> CommandsDocuments { get; set; }
    public DbSet<ElectrostoreAPI.Models.CommandsHistory> CommandsHistory { get; set; }
    public DbSet<ElectrostoreAPI.Models.CommandsItems> CommandsItems { get; set; }
    public DbSet<ElectrostoreAPI.Models.AI> AI { get; set; }
    public DbSet<ElectrostoreAPI.Models.Imgs> Imgs { get; set; }
    public DbSet<ElectrostoreAPI.Models.Items> Items { get; set; }
    public DbSet<ElectrostoreAPI.Models.ItemsBoxs> ItemsBoxs { get; set; }
    public DbSet<ElectrostoreAPI.Models.ItemsHistory> ItemsHistory { get; set; }
    public DbSet<ElectrostoreAPI.Models.ItemsDocuments> ItemsDocuments { get; set; }
    public DbSet<ElectrostoreAPI.Models.ItemsTags> ItemsTags { get; set; }
    public DbSet<ElectrostoreAPI.Models.JwiAccessTokens> JwiAccessTokens { get; set; }
    public DbSet<ElectrostoreAPI.Models.JwiRefreshTokens> JwiRefreshTokens { get; set; }
    public DbSet<ElectrostoreAPI.Models.Leds> Leds { get; set; }
    public DbSet<ElectrostoreAPI.Models.Projects> Projects { get; set; }
    public DbSet<ElectrostoreAPI.Models.ProjectsComments> ProjectsComments { get; set; }
    public DbSet<ElectrostoreAPI.Models.ProjectsDocuments> ProjectsDocuments { get; set; }
    public DbSet<ElectrostoreAPI.Models.ProjectsItems> ProjectsItems { get; set; }
    public DbSet<ElectrostoreAPI.Models.ProjectsProjectTags> ProjectsProjectTags { get; set; }
    public DbSet<ElectrostoreAPI.Models.ProjectTags> ProjectTags { get; set; }
    public DbSet<ElectrostoreAPI.Models.ProjectsStatus> ProjectsStatus { get; set; }
    public DbSet<ElectrostoreAPI.Models.ProjectsSteps> ProjectsSteps { get; set; }
    public DbSet<ElectrostoreAPI.Models.Stores> Stores { get; set; }
    public DbSet<ElectrostoreAPI.Models.StoresTags> StoresTags { get; set; }
    public DbSet<ElectrostoreAPI.Models.Tags> Tags { get; set; }
    public DbSet<ElectrostoreAPI.Models.UserPushSubscriptions> UserPushSubscriptions { get; set; }
    public DbSet<ElectrostoreAPI.Models.Users> Users { get; set; }

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

        modelBuilder.Entity<ProjectsItems>()
            .HasKey(pi => new { pi.id_project, pi.id_item });

        modelBuilder.Entity<ProjectsProjectTags>()
            .HasKey(ib => new { ib.id_project, ib.id_project_tag });

        modelBuilder.Entity<StoresTags>()
            .HasKey(st => new { st.id_store, st.id_tag });

        modelBuilder.Entity<ItemsHistory>()
            .HasOne(h => h.Item)
            .WithMany(i => i.ItemsHistory)
            .HasForeignKey(h => h.id_item)
            .OnDelete(DeleteBehavior.SetNull);

        modelBuilder.Entity<ItemsHistory>()
            .HasOne(h => h.Box)
            .WithMany()
            .HasForeignKey(h => h.id_box)
            .OnDelete(DeleteBehavior.SetNull);

        modelBuilder.Entity<ItemsHistory>()
            .HasOne(h => h.User)
            .WithMany()
            .HasForeignKey(h => h.id_user)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
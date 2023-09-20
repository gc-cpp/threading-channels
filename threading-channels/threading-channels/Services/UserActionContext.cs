using Microsoft.EntityFrameworkCore;
using threading_channels.Services.Models;

namespace threading_channels.Services;

public class UserActionContext : DbContext
{
    public UserActionContext(DbContextOptions dbContextOptions) : base(dbContextOptions)
    {
    }

    public DbSet<UserAction> UserActions { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder
            .Entity<UserAction>()
            .HasKey(x => x.Id);

        modelBuilder
            .Entity<UserAction>()
            .Property(x => x.Id).ValueGeneratedOnAdd();
    }
}
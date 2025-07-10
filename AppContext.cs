using AllTemplates.Domain;
using Microsoft.EntityFrameworkCore;

namespace AllTemplates;

public class AppContext: DbContext
{
	public DbSet<Template> Templates => Set<Template>();
	public DbSet<User> Users => Set<User>();

	public AppContext(DbContextOptions<AppContext> options) : base(options)
	{
	}

	protected override void OnModelCreating(ModelBuilder modelBuilder)
	{
		modelBuilder.Entity<User>()
			.HasMany(u => u.Favorits)
			.WithMany()
			.UsingEntity("Favorits");

		modelBuilder.Entity<Template>()
			.HasOne(t => t.Sender)
			.WithMany();
	}
}
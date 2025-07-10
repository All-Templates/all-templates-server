using Microsoft.EntityFrameworkCore;
using Minio;

namespace AllTemplates
{
	public class Program
	{
		public static void Main(string[] args)
		{
			var builder = WebApplication.CreateBuilder(args);

			builder.Services.AddControllers();

			var conStr = builder.Configuration["ConStr"]!;

			builder.Services.AddDbContext<AppContext>(options => options.UseNpgsql(conStr));

			var minioEndpoint = builder.Configuration.GetSection("Minio")["Endpoint"]!;
			var minioAccessKey = builder.Configuration.GetSection("Minio")["AccessKey"]!;
			var minioSecretKey = builder.Configuration.GetSection("Minio")["SecretKey"]!;

			builder.Services.AddMinio(client => client
				.WithEndpoint(minioEndpoint)
				.WithCredentials(minioAccessKey, minioSecretKey)
				.WithSSL(false)
				.Build()
			);

			builder.Services.AddCors(options => options.AddPolicy("pol", policy => policy.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader()));

			var app = builder.Build();

			var context = app.Services.CreateScope().ServiceProvider.GetRequiredService<AppContext>();
			//context.Database.EnsureDeleted();
			context.Database.EnsureCreated();


			// Configure the HTTP request pipeline.

			app.UseAuthorization();

			app.UseCors("pol");

			app.MapControllers();

			app.Run();
		}
	}
}

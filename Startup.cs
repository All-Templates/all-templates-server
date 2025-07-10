using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Minio;
using PhotoSauce.NativeCodecs.Libjpeg;
using PhotoSauce.NativeCodecs.Libpng;
using System.Runtime.InteropServices;
using System.Text;

namespace AllTemplates;

public class Startup
{
	private IConfiguration config;

	public Startup(IConfiguration config)
	{
		this.config = config;
	}

	public void ConfigureServices(IServiceCollection services)
	{
		services.AddControllers();

		ConfigureDataBasesServices(services);

		services.AddCors(options => options.AddPolicy("pol", policy => policy.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader()));

		var validationParams = new TokenValidationParameters()
		{
			ValidateIssuer = true,
			ValidIssuer = JwtParams.Issuer,
			ValidateAudience = true,
			ValidAudience = JwtParams.Audience,
			ValidateLifetime = true,
			ValidateIssuerSigningKey = true,
			IssuerSigningKey = JwtParams.SecurityKey
		};

		services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
			.AddJwtBearer(options => options.TokenValidationParameters = validationParams);

		services.AddAuthorization();
	}

	public void Configure(WebApplication app, IWebHostEnvironment env)
	{
		ConfigureDataBases(app, env);

		if ( RuntimeInformation.IsOSPlatform(OSPlatform.Linux) )
			PhotoSauce.MagicScaler.CodecManager.Configure(
				codecs =>
				{
					codecs.UseLibjpeg();
					codecs.UseLibpng();
				}
			);

		app
			.UseAuthentication()
			.UseAuthorization();

		app.UseCors("pol");

		app.MapControllers();
	}

	private void ConfigureDataBasesServices(IServiceCollection services)
	{
		var conStr = config["ConStr"]!;

		services.AddDbContext<AppContext>(options => options.UseNpgsql(conStr));

		var minioEndpoint = config.GetSection("Minio")["Endpoint"]!;
		var minioAccessKey = config.GetSection("Minio")["AccessKey"]!;
		var minioSecretKey = config.GetSection("Minio")["SecretKey"]!;

		services.AddMinio(client => client
			.WithEndpoint(minioEndpoint)
			.WithCredentials(minioAccessKey, minioSecretKey)
			.WithSSL(false)
			.Build()
		);
	}

	private void ConfigureDataBases(WebApplication app, IWebHostEnvironment env)
	{
		var context = app.Services.CreateScope().ServiceProvider.GetRequiredService<AppContext>();
		context.Database.Migrate();
	}
}

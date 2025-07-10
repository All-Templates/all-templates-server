using AllTemplates.Domain;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Minio;
using System.Security.Cryptography;
using System.Text;

namespace AllTemplates.Controllers
{
	[ApiController]
	[Route("api/[controller]")]
	public class UserController : ControllerBase
	{

		private readonly ILogger logger;

		private readonly AppContext db;

		public UserController(
			ILogger<TemplatesController> logger,
			AppContext appContext,
			IMinioClient minioClient)
		{
			this.logger = logger;
			db = appContext;
		}

		[HttpGet("register")]
		public IResult Register(string login, string password)
		{
			if ( db.Users.Any(u => u.Login == login) )
			{
				return Results.Conflict("Login already exists");
			}

			var passAsBytes = Encoding.UTF8.GetBytes(password);
			var hashedPass = Convert.ToHexString( SHA256.HashData(passAsBytes) );

			var user = new User()
			{
				Login = login,
				HashedPass = hashedPass,
			};

			db.Users.Add(user);

			db.SaveChanges();

			return Results.Ok(user.Id);
		}

		[HttpGet]
		public IResult Login(string login, string password)
		{
			var passAsBytes = Encoding.UTF8.GetBytes(password);
			var hashedPass = Convert.ToHexString(SHA256.HashData(passAsBytes));

			var user = db.Users.FirstOrDefault(u => u.Login == login && u.HashedPass == hashedPass);

			if (user == null)
				return Results.NotFound();

			return Results.Ok(user.Id);
		}

		[HttpGet("{userId}/favs")]
		public IResult Favs(int userId)
		{
			var templateIds = db.Users
				.Include(u => u.Favorits)
				.First(u => u.Id == userId)
				.Favorits
				.Select(t => t.Id);

			return Results.Json(templateIds);
		}

		[HttpGet("{userId}/favs/add")]
		public IResult AddFave(int userId, [FromQuery(Name = "template")] int templateId)
		{
			var user = db.Users.First(u => u.Id == userId);
			var template = db.Templates.First(t => t.Id == templateId);

			user.Favorits.Add(template);

			db.SaveChanges();

			return Results.Ok();
		}

		[HttpGet("{userId}/favs/remove")]
		public IResult RemoveFave(int userId, [FromQuery(Name = "template")] int templateId)
		{
			var user = db.Users
				.Include(u => u.Favorits)
				.First(u => u.Id == userId);

			var template = user.Favorits.FirstOrDefault(item => item.Id == templateId);

			if (template == null)
			{
				return Results.Conflict("");
			}

			user.Favorits.Remove(template);

			db.SaveChanges();

			return Results.Ok();
		}

		[HttpGet("{userId}/uploads")]
		public IResult Uploads(int userId)
		{
			var templates = db.Templates
				.Where(item => item.SenderId == userId)
				.Select(item => item.Id);

			return Results.Json(templates);
		}
	}
}

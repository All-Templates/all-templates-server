using AllTemplates.Domain;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace AllTemplates.Controllers;

[ApiController]
[Route("[controller]")]
public class UserController : ControllerBase
{
	private readonly ILogger logger;

	private readonly AppContext db;

	public UserController(
		ILogger<TemplatesController> logger,
		AppContext appContext)
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

		var claims = new Claim[]
		{
			new Claim(ClaimTypes.NameIdentifier, user.Id.ToString())
		};

		var jwt = new JwtSecurityToken(
			JwtParams.Issuer,
			JwtParams.Audience,
			claims,
			expires: DateTime.Now.AddDays(1),
			signingCredentials: new SigningCredentials(JwtParams.SecurityKey, SecurityAlgorithms.HmacSha256)
		);

		return Results.Text(new JwtSecurityTokenHandler().WriteToken(jwt));
	}

	[HttpGet("login")]
	public IResult Login(string login, string password)
	{
		var passAsBytes = Encoding.UTF8.GetBytes(password);
		var hashedPass = Convert.ToHexString(SHA256.HashData(passAsBytes));

		var user = db.Users.FirstOrDefault(u => u.Login == login && u.HashedPass == hashedPass);

		if (user == null)
			return Results.NotFound();

		var claims = new List<Claim>()
		{
			new Claim(ClaimTypes.NameIdentifier, user.Id.ToString())
		};

		if (user.IsAdmin)
			claims.Add( new Claim(ClaimTypes.Role, "Admin") );

		var jwt = new JwtSecurityToken(
			JwtParams.Issuer,
			JwtParams.Audience,
			claims,
			expires: DateTime.Now.AddDays(1),
			signingCredentials: new SigningCredentials(JwtParams.SecurityKey, SecurityAlgorithms.HmacSha256)
		);
		
		return Results.Text(new JwtSecurityTokenHandler().WriteToken(jwt));
	}

	[HttpGet("favs")]
	[Authorize]
	public IResult Favs()
	{
		var userIdClaim = HttpContext.User.FindFirst(ClaimTypes.NameIdentifier)!;

		var userId = int.Parse(userIdClaim.Value);
			
		var templateIds = db.Users
			.Include(u => u.Favorits)
			.First(u => u.Id == userId)
			.Favorits
			.Select(t => t.Id);

		return Results.Json(templateIds);
	}

	[HttpGet("favs/add")]
	[Authorize]
	public IResult AddFave([FromQuery(Name = "template")] int templateId)
	{
		var userIdClaim = HttpContext.User.FindFirst(ClaimTypes.NameIdentifier)!;

		var userId = int.Parse(userIdClaim.Value);

		var user = db.Users
			.Include(u => u.Favorits)
			.FirstOrDefault(u => u.Id == userId);

		if (user == null)
			return Results.NotFound("User not found");

		var template = db.Templates.FirstOrDefault(t => t.Id == templateId);

		if (template == null)
			return Results.NotFound("Template not found");

		if (user.Favorits.Any(fave => fave.Id == template.Id))
			return Results.Ok("Template already added");

		user.Favorits.Add(template);

		db.SaveChanges();

		return Results.Ok();
	}

	[HttpGet("favs/remove")]
	[Authorize]
	public IResult RemoveFave([FromQuery(Name = "template")] int templateId)
	{
		var userIdClaim = HttpContext.User.FindFirst(ClaimTypes.NameIdentifier)!;

		var userId = int.Parse(userIdClaim.Value);

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

	[HttpGet("uploads")]
	[Authorize]
	public IResult Uploads()
	{
		var userIdClaim = HttpContext.User.FindFirst(ClaimTypes.NameIdentifier)!;

		var userId = int.Parse(userIdClaim.Value);

		var templates = db.Templates
			.Where(item => item.SenderId == userId)
			.Select(item => item.Id);

		return Results.Json(templates);
	}
}
using AllTemplates.Domain;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Minio;
using Minio.DataModel.Args;
using System.Security.Claims;
using PhotoSauce.MagicScaler;

namespace AllTemplates.Controllers;

[ApiController]
[Route("api/[controller]")]
public class TemplatesController : ControllerBase
{
	private readonly ILogger logger;

	private readonly AppContext db;
	private readonly IMinioClient minioClient;

	public TemplatesController(
		ILogger<TemplatesController> logger,
		AppContext appContext,
		IMinioClient minioClient)
	{
		this.logger = logger;
		db = appContext;
		this.minioClient = minioClient;
	}

	[HttpGet]
	public IResult Get(int? offset, int? limit)
	{
		var templates = db.Templates.Where(item => item.State == TemplateStates.Approved);

		if (offset.HasValue && limit.HasValue)
		{
			templates = templates.Skip(offset.Value).Take(limit.Value);
		}

		var templateIds = templates.Select(templates => templates.Id);

		return Results.Json(templateIds);
	}

	[HttpGet("{id:int}")]
	public IResult Get(int id)
	{
		var template = db.Templates.SingleOrDefault(item => item.Id == id);

		if (template == null)
			return Results.NotFound();

		var result = new { id = template.Id, keyWords = template.KeyWords };

		return Results.Json(result);
	}

	[HttpGet("search")]
	public IResult Search(string q)
	{
		var keyWords = q.Split( [',', ' ', '.'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries );

		var allowedTemplates = db.Templates
			.Where(item => item.State == TemplateStates.Approved);

		if ( HttpContext.User.HasClaim(claim => claim.Type == ClaimTypes.NameIdentifier) )
		{
			var userId = int.Parse( HttpContext.User.FindFirstValue(ClaimTypes.NameIdentifier)! );
			allowedTemplates = allowedTemplates
				.Union( db.Templates.Where(item => item.SenderId == userId) );
		}

		var templateIds = allowedTemplates
			.Where(item => item.KeyWords.Intersect(keyWords).Count() > 0)
			.OrderByDescending(item => item.KeyWords.Intersect(keyWords).Count())
			.Select(item => item.Id);

		return Results.Json(templateIds);
	}

	[HttpGet("unchecked")]
	[Authorize(Roles = "Admin")]
	public IResult Unchecked()
	{
		var templates = db.Templates
			.Where(item => item.State == TemplateStates.Unchecked)
			.Select(item => item.Id);

		return Results.Json(templates);
	}

	[HttpGet("approve/{id:int}")]
	[Authorize(Roles = "Admin")]
	public IResult Approve(int id)
	{
		var template = db.Templates.SingleOrDefault(item => item.Id == id);

		if (template == null)
			return Results.NotFound();

		template.State = TemplateStates.Approved;

		db.SaveChanges();

		return Results.Ok();
	}

	[HttpGet("reject/{id:int}")]
	[Authorize(Roles = "Admin")]
	public IResult Reject(int id)
	{
		var template = db.Templates.SingleOrDefault(item => item.Id == id);

		if (template == null)
			return Results.NotFound();

		template.State = TemplateStates.Rejected;

		db.SaveChanges();

		return Results.Ok();
	}

	[HttpPost("create")]
	public async Task<IResult> Create(
		[FromForm] string keyWords,
		[FromForm] IFormFile pic,
		[FromForm] bool notForPublic = false)
	{
		int? userId = null;

		if (HttpContext.User.Identity != null && HttpContext.User.Identity.IsAuthenticated)
		{
			var userIdClaim = HttpContext.User.FindFirst(ClaimTypes.NameIdentifier)!;
			userId = int.Parse(userIdClaim.Value);
		}

		var template = new Template()
		{
			KeyWords = keyWords.Split( [',', ' '], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries ),
			SenderId = userId
		};

		if (notForPublic)
			template.State = TemplateStates.NonForPublic;

		db.Templates.Add(template);

		db.SaveChanges();

		using var picStream = pic.OpenReadStream();

		if ( !await minioClient.BucketExistsAsync(new BucketExistsArgs().WithBucket("templates")) )
		{
			await minioClient.MakeBucketAsync(new MakeBucketArgs().WithBucket("templates"));
		}

		var minioPutArgs = new PutObjectArgs()
			.WithBucket("templates")
			.WithObject(template.Id.ToString())
			.WithStreamData(picStream)
			.WithObjectSize(picStream.Length);

		await minioClient.PutObjectAsync(minioPutArgs);

		return Results.Text(template.Id.ToString());
	}

	[HttpGet("download/{id:int}")]
	public async Task<IResult> Download(int id, bool isPreview = false)
	{
		if (!db.Templates.Any(t => t.Id == id))
			return Results.NotFound();

		var originalPic = new MemoryStream();

		var minioGetArgs = new GetObjectArgs()
			.WithBucket("templates")
			.WithObject(id.ToString())
			.WithCallbackStream(cb =>
				{
					cb.CopyTo(originalPic);
					originalPic.Position = 0;
				}
			);

		await minioClient.GetObjectAsync(minioGetArgs);

		var outputPic = new MemoryStream();

		if (isPreview)
		{
			MagicImageProcessor
				.ProcessImage(originalPic, outputPic, new ProcessImageSettings() { Width = 400 });

			outputPic.Position = 0;
		}
		else
			outputPic = originalPic;

		return Results.File(outputPic);
	}
}
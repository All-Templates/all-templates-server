using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AllTemplates.Controllers
{
	[ApiController]
	[Route("[controller]")]
	public class TestController: ControllerBase
	{
		AppContext db;

		public TestController(AppContext appContext) 
		{ 
			db = appContext;
		}


		[HttpGet]
		public IResult Get()
		{
			var users = db.Users.Include(u => u.Favorits).ToList();


			return Results.Ok();
		}

	}
}

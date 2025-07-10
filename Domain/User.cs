namespace AllTemplates.Domain;

public class User
{
	public int Id { get; set; }
	public required string Login { get; set; }
	public required string HashedPass { get; set; }
	public List<Template> Favorits { get; set; } = new List<Template>();
	public bool IsAdmin { get; set; }
}

namespace AllTemplates.Domain;

public class Template
{
	public int Id { get; set; }
	public TemplateStates State { get; set; } = TemplateStates.Unchecked;

	public required IList<string> KeyWords { get; set; }

	public int? SenderId { get; set; }
	public User? Sender { get; set; }

	public Template() {}
}

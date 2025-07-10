using Microsoft.IdentityModel.Tokens;
using System.Text;

namespace AllTemplates;

public class JwtParams
{
	public const string Issuer = "abobus";
	public const string Audience = "abobus";

	private const string key = "mysupersecret_secretsecretsecretkey!123";

	public static readonly SymmetricSecurityKey SecurityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key));
}

using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;

namespace Hotfix.Utils;

public static class JwtHelper
{
    private static string LoadSecret()
    {
        return Environment.GetEnvironmentVariable("JWT_SECRET")
               ?? throw new InvalidOperationException("JWT_SECRET 未在环境变量中配置");
    }

    public static string SignToken(long userId)
    {
        var secret = LoadSecret();
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
        };

        var token = new JwtSecurityToken(
            claims: claims,
            expires: DateTime.UtcNow.AddHours(24),
            signingCredentials: credentials
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}

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

    /// <summary>验证 token 有效性，失败返回 null</summary>
    public static ClaimsPrincipal? ValidateToken(string token)
    {
        var secret = LoadSecret();
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret));

        try
        {
            var handler = new JwtSecurityTokenHandler();
            return handler.ValidateToken(token, new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = key,
                ValidateIssuer = false,
                ValidateAudience = false,
                ValidateLifetime = true,
                ClockSkew = TimeSpan.Zero
            }, out _);
        }
        catch
        {
            return null;
        }
    }

    /// <summary>从 token 中取出 userId，无效返回 null</summary>
    public static long? GetUserIdFromToken(string token)
    {
        var principal = ValidateToken(token);
        if (principal == null) return null;

        var claim = principal.FindFirst(ClaimTypes.NameIdentifier);
        if (claim == null) return null;

        return long.Parse(claim.Value);
    }
}

using Entity.DTOs;
using Entity.Models;
using Hotfix.Repositories;
using Hotfix.Utils;

namespace Hotfix.Services;

public class AuthService
{
    public (bool Success, string Message) Register(RegisterRequest request)
    {
        var existingUser = UserDao.FindByEmail(request.Email);
        if (existingUser != null)
        {
            return (false, "该邮箱已被注册");
        }

        var salt = PasswordHelper.GenerateSalt();

        var user = new User
        {
            Uuid = IdGenerator.CreateUuid(),
            Email = request.Email,
            Username = request.Username,
            Salt = salt,
            PasswordHash = PasswordHelper.Hash(request.Password, salt),
            CreatedAt = DateTime.UtcNow,
            LastLoginAt = DateTime.UtcNow,
            IsDeleted = false
        };

        UserDao.Insert(user);

        return (true, "注册成功");
    }
}

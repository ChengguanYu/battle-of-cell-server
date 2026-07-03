using System.Collections.Generic;
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

    public Dictionary<string, object?> Login(LoginRequest request)
    {
        // 判断 account 类型：优先尝试解析为 long（id），否则视为 email
        User? user;
        if (long.TryParse(request.Account, out var id))
        {
            user = UserDao.FindById(id);
        }
        else
        {
            user = UserDao.FindByEmail(request.Account);
        }

        if (user == null)
        {
            return new Dictionary<string, object?>
            {
                ["code"] = (int)ResponseCode.AccountNotFound,
                ["message"] = "账号不存在"
            };
        }

        var hash = PasswordHelper.Hash(request.Password, user.Salt);
        if (hash != user.PasswordHash)
        {
            return new Dictionary<string, object?>
            {
                ["code"] = (int)ResponseCode.InvalidPassword,
                ["message"] = "密码错误"
            };
        }

        var token = JwtHelper.SignToken(user.Id);

        return new Dictionary<string, object?>
        {
            ["code"] = (int)ResponseCode.Success,
            ["message"] = "登录成功",
            ["extra"] = new Dictionary<string, object?>
            {
                ["token"] = token,
                ["user"] = new
                {
                    uuid = user.Uuid,
                    username = user.Username,
                    email = user.Email,
                    createdAt = user.CreatedAt
                }
            }
        };
    }
}

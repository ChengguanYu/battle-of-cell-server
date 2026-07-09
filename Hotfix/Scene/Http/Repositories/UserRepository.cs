using Entity.Models;
using Fantasy.Async;
using Hotfix.Database;

namespace Hotfix.Scene.Http.Repositories;

public static class UserDao
{
    public static User? FindByEmail(string email)
    {
        return DbManager.GetInstance().Queryable<User>().First(u => u.Email == email);
    }

    public static User? FindById(long id)
    {
        return DbManager.GetInstance().Queryable<User>().First(u => u.Id == id);
    }

    public static async FTask<User?> FindByIdAsync(long id)
    {
        return await DbManager.GetInstance().Queryable<User>().FirstAsync(u => u.Id == id);
    }

    public static void Insert(User user)
    {
        DbManager.GetInstance().Insertable(user).ExecuteCommand();
    }
}

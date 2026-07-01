using SqlSugar;

namespace Entity.Models;

[SugarTable("users")]
public class User : EntityBase
{
    [SugarColumn(IsPrimaryKey = true, IsIdentity = true)]
    public long Id { get; set; }

    [SugarColumn(Length = 64, IsNullable = false)]
    public string Username { get; set; } = string.Empty;

    [SugarColumn(Length = 256, IsNullable = false)]
    public string PasswordHash { get; set; } = string.Empty;

    [SugarColumn(Length = 256, IsNullable = false)]
    public string Password { get; set; } = string.Empty;

    public DateTime? LastLoginAt { get; set; }
}
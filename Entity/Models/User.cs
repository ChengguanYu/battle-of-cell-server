using SqlSugar;

namespace Entity.Models;

[SugarTable("users")]
[SugarIndex("uq_user_uuid", nameof(Uuid), OrderByType.Asc, true)]
public class User : EntityBase
{
    [SugarColumn(ColumnName = "s_user_id", IsPrimaryKey = true, IsIdentity = true)]
    public long Id { get; set; }

    [SugarColumn(ColumnDataType = "uuid", IsNullable = false)]
    public Guid Uuid { get; set; }
    
    [SugarColumn(Length = 256, IsNullable = false)]
    public string Email { get; set; } = string.Empty;

    [SugarColumn(Length = 64, IsNullable = false)]
    public string Username { get; set; } = string.Empty;

    [SugarColumn(Length = 256, IsNullable = false)]
    public string PasswordHash { get; set; } = string.Empty;

    [SugarColumn(Length = 256, IsNullable = false)]
    public string Salt { get; set; } = string.Empty;

    public DateTime? LastLoginAt { get; set; }
}
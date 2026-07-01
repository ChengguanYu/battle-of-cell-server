using SqlSugar;

namespace Entity.Models;

public abstract class EntityBase
{
    [SugarColumn(IsNullable = false)]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [SugarColumn(IsNullable = false)]
    public bool IsDeleted { get; set; }
}
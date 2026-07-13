namespace Entity.Domains;
public interface IDomainBase<T>
{
    // 加载原型
    public void Load(T domainProto);
}
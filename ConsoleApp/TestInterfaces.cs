using Jay.SourceGen.InterfaceGen.Attributes;

namespace Jay.SourceGen.ConsoleApp;

[Implement]
public interface IEntity : IEquatable<IEntity>, IComparable<IEntity>//, IFormattable
{
    [Key]
    int Id { get; init; }
}

//[Implement]
//public interface IKeyEntity<TKey> : IEntity,
//    IEquatable<IKeyEntity<TKey>>,
//    IComparable<IKeyEntity<TKey>>
//    where TKey : notnull, IEquatable<TKey>, IComparable<TKey>
//{
//    TKey Id { get; init; }
//}
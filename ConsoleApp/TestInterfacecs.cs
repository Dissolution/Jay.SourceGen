using Jay.SourceGen.InterfaceGen.Attributes;

namespace Jay.SourceGen.ConsoleApp;

[Implement]
public interface IEntity : IFormattable
{

}

//[Implement]
//public interface IKeyEntity<TKey> : IEntity,
//    IEquatable<IKeyEntity<TKey>>,
//    IComparable<IKeyEntity<TKey>>
//    where TKey : notnull, IEquatable<TKey>, IComparable<TKey>
//{
//    TKey Id { get; init; }
//}
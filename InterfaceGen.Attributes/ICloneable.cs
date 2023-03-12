namespace Jay.SourceGen.InterfaceGen.Attributes;

public interface ICloneable<TSelf> : ICloneable
    where TSelf : ICloneable<TSelf>
{
    new TSelf Clone();
}

namespace Jay.SourceGen.InterfaceGen.Attributes;

public enum CloneDepth
{
    /// <summary>
    /// A shallow copy will copy values and point to references
    /// </summary>
    Shallow = 0,

    /// <summary>
    /// A deep copy will copy values and copy references (the entire tree)
    /// </summary>
    Deep = 1,
}

public interface ICloneable<TSelf> : ICloneable
    where TSelf : ICloneable<TSelf>
{
    TSelf Clone(CloneDepth cloneDepth = CloneDepth.Shallow);
}

namespace Jay.SourceGen.InterfaceGen.Attributes;

[AttributeUsage(AttributeTargets.Property | AttributeTargets.Event)]
public class DisposeAttribute : Attribute
{
    public DisposeAttribute() { }
}

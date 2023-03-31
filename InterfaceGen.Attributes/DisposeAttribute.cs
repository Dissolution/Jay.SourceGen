namespace Jay.SourceGen.InterfaceGen.Attributes;

[AttributeUsage(AttributeTargets.Property | AttributeTargets.Event, AllowMultiple = false, Inherited = true)]
public class DisposeAttribute : Attribute
{
    public DisposeAttribute() { }
}

namespace Jay.SourceGen.InterfaceGen.Attributes;

[AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
public class KeyAttribute : Attribute
{
    public KeyAttribute() { }
}

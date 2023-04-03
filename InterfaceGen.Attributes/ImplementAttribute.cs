namespace Jay.SourceGen.InterfaceGen.Attributes;

[AttributeUsage(AttributeTargets.Interface, AllowMultiple = false, Inherited = false)]
public class ImplementAttribute : Attribute
{
    public string? Keywords { get; set; } = null;
    public string? Name { get; set; } = null;

    public ImplementAttribute()
    {

    }
}
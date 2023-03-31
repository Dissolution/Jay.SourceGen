namespace Jay.SourceGen.InterfaceGen.Attributes;

[AttributeUsage(AttributeTargets.Interface, AllowMultiple = false, Inherited = true)]
public class ImplementAttribute : Attribute
{
    public string? Name {get;set;} = null;
    public bool IsClass { get; set; } = true;
    public bool IsPartial { get; set; } = false;
    public bool IsAbstract { get; set; } = false;
    public bool IsSealed { get; set; } = true;

    public ImplementAttribute()
    {

    }
}
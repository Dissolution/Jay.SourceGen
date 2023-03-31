namespace Jay.SourceGen.InterfaceGen.Attributes;

[AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
public class DisplayAttribute : Attribute
{
    public string? Format { get; init; } = null;

    public DisplayAttribute(string? format = null)
    {
        this.Format = format;
    }
}

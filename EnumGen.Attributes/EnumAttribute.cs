namespace Jay.SourceGen.EnumGen.Attributes;

[AttributeUsage(AttributeTargets.Struct, AllowMultiple = false, Inherited = true)]
public sealed class EnumAttribute : Attribute
{
    public bool Flags { get; init; } = false;

    public EnumAttribute(bool flags = false)
    {
        this.Flags = flags;
    }
}
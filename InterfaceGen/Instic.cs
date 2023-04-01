namespace Jay.SourceGen.InterfaceGen;

[Flags]
public enum Instic
{
    Static = 1 << 0,
    Instance = 1 << 1,

    Any = Static | Instance,
}

internal static class InsticExtensions
{
    public static void DeclareTo(this Instic instic, CodeBuilder codeBuilder)
    {
        if (instic == Instic.Static)
        {
            codeBuilder.Append("static ");
        }
    }
}
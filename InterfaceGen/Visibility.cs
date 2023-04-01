namespace Jay.SourceGen.InterfaceGen;

[Flags]
public enum Visibility
{
    Private = 1 << 0,
    Protected = 1 << 1,
    Internal = 1 << 2,
    Public = 1 << 3,

    Any = Private | Protected | Internal | Public,
}

internal static class VisibilityExtensions
{
    public static void DeclareTo(this Visibility visibility, CodeBuilder codeBuilder)
    {
        codeBuilder.Enumerate(
            visibility.GetFlags(),
            static (cb, v) => cb.Append(v.ToString().ToLower()).Append(' '));
    }
}

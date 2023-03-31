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

namespace Jay.SourceGen.InterfaceGen;

[Flags]
public enum Instic
{
    Static = 1 << 0,
    Instance = 1 << 1,

    Any = Static | Instance,
}

namespace Jay.SourceGen.InterfaceGen;

[Flags]
public enum ObjType
{
    Struct = 1 << 0,
    Class = 1 << 1,

    Any = Struct | Class,
}

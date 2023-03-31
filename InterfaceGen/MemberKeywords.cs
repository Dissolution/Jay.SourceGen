namespace Jay.SourceGen.InterfaceGen;

[Flags]
public enum MemberKeywords
{
    Virtual = 1 << 0,
    Abstract = 1 << 1,
    Sealed = 1 << 2,
    Partial = 1 << 3,
}

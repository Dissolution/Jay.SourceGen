namespace Jay.SourceGen.InterfaceGen;

[Flags]
public enum MemberType
{
    Field = 1 << 0,
    Property = 1 << 1,
    Event = 1 << 2,
    Method = 1 << 3,
    Constructor = 1 << 4 | Method,
    Operator = 1 << 5 | Method,

    Any = Field | Property | Event | Method | Constructor | Operator,
}

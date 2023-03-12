using Microsoft.CodeAnalysis;

namespace Jaynums.SourceGen;

public class EnumStructToGenerate
{
    public bool Flags { get; init; } = false;
    public required ITypeSymbol Type { get; init; }
    public required IReadOnlyList<(string Name, ITypeSymbol Type)> InstanceFields { get; init; }
    public required IReadOnlyList<string> EnumMembers { get; init; }

    public bool HasToString { get; init; } = false;
    public bool SkipZero { get; init; } = false;

    public int FieldCount => InstanceFields.Count;
    public int MembersLength => EnumMembers.Count + (SkipZero ? 1 : 0);

    public EnumStructToGenerate()
    {

    }
}

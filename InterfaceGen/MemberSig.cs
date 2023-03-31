using System.Collections.Immutable;

namespace Jay.SourceGen.InterfaceGen;

public sealed record MemberSig(
    Instic Instic,
    Visibility Visibility,
    MemberType Type,
    ImmutableArray<AttributeData> Attributes,
    string Name,
    ITypeSymbol ReturnType,
    ImmutableArray<IParameterSymbol> ParamTypes);

namespace Jay.SourceGen.EntityGen;

internal sealed class EntityInfo
{
    public required ITypeSymbol Type { get; init; }
    public required IReadOnlyList<MemberSymbolInfo> Members { get; init; }
    public required bool IsSealed { get; init; }
    public bool IsNotify { get; init; } = false;

    public string? NameSpace => Type.GetNamespace();
    public string TypeName => Type.Name;

    public string VarName => Type.Name.ToVariableName();
    
    // public IReadOnlyList<EntityMemberInfo> MembersWithAttribute<TAttribute>()
    //     where TAttribute : Attribute
    // {
    //     return Members.Where(member => member.AttributeData.ContainsKey(nameof(TAttribute))).ToList();
    // }
    //
    //  public IReadOnlyList<EntityMemberInfo> MembersWithAttribute(string attributeName)
    // {
    //     return Members.Where(member => member.AttributeData.ContainsKey(attributeName)).ToList();
    // }
}

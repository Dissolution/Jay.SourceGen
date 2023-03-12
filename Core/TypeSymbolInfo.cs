
using Microsoft.CodeAnalysis.CSharp;

namespace Jay.SourceGen;

public class TypeSymbolInfo
{
    protected readonly TypeDeclarationSyntax _typeDeclarationSyntax;
    protected readonly INamedTypeSymbol _typeSymbol;

    public string Name => _typeSymbol.Name;
    public string VarName => _typeSymbol.GetVariableName();
    public ITypeSymbol Type => _typeSymbol;
    public IReadOnlyList<MemberSymbolInfo> Members { get; }
    public SymbolAttributeData Attributes { get; }
    public ImmutableArray<INamedTypeSymbol> Interfaces => _typeSymbol.Interfaces;
    public bool HasKeyword(SyntaxKind keyword) => _typeDeclarationSyntax.HasKeyword(keyword);

    public bool IsStruct => _typeDeclarationSyntax is StructDeclarationSyntax;
    public bool IsClass => _typeDeclarationSyntax is ClassDeclarationSyntax;
    public bool IsInterface => _typeDeclarationSyntax is InterfaceDeclarationSyntax;

    public TypeSymbolInfo(
        TypeDeclarationSyntax typeDeclarationSyntax,
        INamedTypeSymbol typeSymbol)
    {
        _typeDeclarationSyntax = typeDeclarationSyntax;
        _typeSymbol = typeSymbol;
        this.Attributes = new(typeSymbol.GetAttributes());
        //var memberSyntaxes = typeDeclarationSyntax.Members;
        var memberSymbols = typeSymbol.GetMembers();
        var msi = new MemberSymbolInfo[memberSymbols.Length];
        for (var i = 0; i < memberSymbols.Length; i++)
        {
            //var memberSyntax = memberSyntaxes[i];
            var memberSymbol = memberSymbols[i];

            msi[i] = new(memberSymbol);
        }
        this.Members = msi;
    }
}

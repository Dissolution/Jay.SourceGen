using Microsoft.CodeAnalysis.CSharp;

namespace Jay.SourceGen;

public class MemberSymbolInfo
{
    //protected readonly MemberDeclarationSyntax _memberDeclaration;
    protected readonly ISymbol _memberSymbol;

    public string Name => _memberSymbol.Name;
    public SymbolAttributeData Attributes { get; }

    //public bool HasKeyword(SyntaxKind keyword)
    //{
    //    return _memberDeclaration.Modifiers.Any(keyword);
    //}

    public ITypeSymbol ReturnType
    {
        get
        {
            return _memberSymbol switch
            {
                IFieldSymbol field => field.Type,
                IPropertySymbol property => property.Type,
                IEventSymbol @event => @event.Type,
                IMethodSymbol method => method.ReturnType,
                _ => throw new InvalidOperationException(),
            };
        }
    }
     public ImmutableArray<IParameterSymbol> Parameters
    {
        get
        {
            return _memberSymbol switch
            {
                IFieldSymbol field => ImmutableArray<IParameterSymbol>.Empty,
                IPropertySymbol property => property.Parameters,
                IEventSymbol @event => ImmutableArray<IParameterSymbol>.Empty,
                IMethodSymbol method => method.Parameters,
                _ => throw new InvalidOperationException(),
            };
        }
    }

    public MemberSymbolInfo(ISymbol symbol)
    {
        //_memberDeclaration = syntax;
        _memberSymbol = symbol;
        this.Attributes = new(symbol.GetAttributes());
    }

}
using Jay.SourceGen.Comparison;

using System.Collections.Immutable;

namespace Jay.SourceGen.InterfaceGen;

public abstract class SymbolSig : MemberSig
{
    public SymbolSig(ISymbol memberSymbol)
    {
        this.Instic = memberSymbol.IsStatic ? Instic.Static : Instic.Instance;
        switch (memberSymbol.DeclaredAccessibility)
        {
            case Accessibility.NotApplicable:
                this.Visibility = default;
                break;
            case Accessibility.Private:
                this.Visibility = Visibility.Private;
                break;
            case Accessibility.ProtectedAndInternal:
                this.Visibility = Visibility.Protected | Visibility.Internal;
                break;
            case Accessibility.Protected:
                this.Visibility = Visibility.Protected;
                break;
            case Accessibility.Internal:
                this.Visibility = Visibility.Internal;
                break;
            case Accessibility.ProtectedOrInternal:
                this.Visibility = Visibility.Protected | Visibility.Internal;
                break;
            case Accessibility.Public:
                this.Visibility = Visibility.Public;
                break;
        }
        this.Attributes = memberSymbol.GetAttributes();
        this.Name = memberSymbol.Name;
    }
}

public class MemberSig : IEquatable<MemberSig>
{
    public static MemberSig FromSymbol(ISymbol memberSymbol)
    {
        switch (memberSymbol)
        {
            case IFieldSymbol fieldSymbol:
                break;
            case IPropertySymbol propertySymbol:
                return new PropertySig(propertySymbol);
            case IEventSymbol eventSymbol:
                break;
            case IMethodSymbol methodSymbol:
                return new MethodSig(methodSymbol);
            default:
                throw new NotImplementedException();
        }
        throw new NotImplementedException();
    }
    public static MemberSig FromImplementation(
        Visibility visibility,
        Instic instic,
        MemberKeywords memberKeywords,
        string name,
        MemberType memberType,
        ITypeSymbol returnType,
        ImmutableArray<IParameterSymbol> paramTypes)
    {
        return new MemberSig
        {
            Visibility = visibility,
            Instic = instic,
            Keywords  = memberKeywords,
            Name = name,
            MemberType = memberType,
            ReturnType = returnType,
            ParamTypes = paramTypes,
        };
    }


    public Visibility Visibility { get; init; }
    public Instic Instic { get; init; }
    public MemberKeywords Keywords { get; init; }

    public ImmutableArray<AttributeData> Attributes { get; init; }
    public string Name { get; init; }
    public string VarName => this.Name.ToVariableName();

    public MemberType MemberType { get; init; }
    public ITypeSymbol ReturnType { get; init; }
    public ImmutableArray<IParameterSymbol> ParamTypes { get; init; }

    public bool HasAttribute(string attributeFQN)
    {
        return this.Attributes.Any(attr => attr.AttributeClass?.GetFQN() == attributeFQN);
    }

    public virtual void WriteDeclaration(CodeBuilder codeBuilder)
    {
        this.Visibility.DeclareTo(codeBuilder);
        this.Instic.DeclareTo(codeBuilder);
        this.Keywords.DeclareTo(codeBuilder);
        codeBuilder.Value(ReturnType).Append(' ')
            .Append(Name)
            .Append('(')
            .Delimit(", ", ParamTypes, static (cb, p) => cb.Value(p))
            .Append(')');
    }

    public bool Equals(MemberSig? memberSig)
    {
        if (memberSig is null) return false;
        if (this.Visibility == memberSig.Visibility &&
            this.Instic == memberSig.Instic &&
            this.Keywords == memberSig.Keywords &&
            this.Name == memberSig.Name &&
            this.MemberType == memberSig.MemberType &&
            this.ReturnType.Equals(memberSig.ReturnType, SymbolEqualityComparer.Default)
        {
            var attrComp = new EnumerableEqualityComparer<AttributeData>(
                new FuncEqualityComparer<AttributeData>(
                    (x,y) => x.GetFullTypeName() == y.GetFullTypeName(),
                    k => k.GetFullTypeName()?.GetHashCode() ?? 0));
            if (!attrComp.Equals(this.Attributes, memberSig.Attributes)) return false;
            var paramComp = new EnumerableEqualityComparer<IParameterSymbol>(
                SymbolEqualityComparer.Default);
            if (!paramComp.Equals(this.ParamTypes, memberSig.ParamTypes)) return false;
            return true;
        }
        return false;
    }
}


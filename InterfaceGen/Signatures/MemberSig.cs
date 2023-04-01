using Jay.SourceGen.Comparison;

using Microsoft.CodeAnalysis;

using System.Collections.Immutable;
using System.Reflection;

namespace Jay.SourceGen.InterfaceGen;

public class TypeSig :
    IEquatable<TypeSig>,
    IEquatable<ITypeSymbol>,
    IEquatable<Type>
{
    public static implicit operator TypeSig(Type type) => new TypeSig(type);

    public string Name { get; }
    public string FullName { get; }

    public TypeSig(ITypeSymbol typeSymbol)
    {
        this.Name = typeSymbol.Name;
        this.FullName = typeSymbol.GetFQN();
    }

    public TypeSig(Type type)
    {
        this.Name = type.Name;
        this.FullName = type.FullName;
    }

    public bool Equals(TypeSig? typeSig)
    {
        return string.Equals(this.FullName, typeSig?.FullName);
    }

    public bool Equals(ITypeSymbol? typeSymbol)
    {
        return string.Equals(this.FullName, typeSymbol?.GetFQN());
    }

    public bool Equals(Type? type)
    {
        return string.Equals(this.FullName, type?.FullName);
    }

    public bool Equals<T>() => Equals(typeof(T));

    public override bool Equals(object? obj)
    {
        if (obj is TypeSig typeSig) return Equals(typeSig);
        if (obj is ITypeSymbol typeSymbol) return Equals(typeSymbol);
        if (obj is Type type) return Equals(type);
        return false;
    }

    public override int GetHashCode()
    {
        return Hasher.Create(FullName);
    }

    public override string ToString()
    {
        return FullName;
    }
}

public class ParameterSig :
    IEquatable<ParameterSig>,
    IEquatable<IParameterSymbol>,
    IEquatable<ParameterInfo>
{
    public string Name { get; }
    public TypeSig Type { get; }

    public bool IsParams { get; }
    public bool HasDefault { get; }
    public object? Default { get; }

    public ParameterSig(IParameterSymbol parameterSymbol)
    {
        this.Name = parameterSymbol.Name;
        this.Type = new(parameterSymbol.Type);
        this.IsParams = parameterSymbol.IsParams;
        this.HasDefault = parameterSymbol.HasExplicitDefaultValue;
        this.Default = parameterSymbol.ExplicitDefaultValue;
    }

    public ParameterSig(ParameterInfo parameterInfo)
    {
        this.Name = parameterInfo.Name;
        this.Type = new(parameterInfo.ParameterType);
        this.IsParams = parameterInfo.GetCustomAttribute<ParamArrayAttribute>() != null;
        this.HasDefault = parameterInfo.HasDefaultValue;
        this.Default = parameterInfo.DefaultValue;
    }

    public bool Equals(ParameterSig? parameterSig)
    {
        return parameterSig is not null &&
           string.Equals(this.Name, parameterSig.Name) &&
           this.Type.Equals(parameterSig.Type) &&
           this.IsParams == parameterSig.IsParams &&
           this.HasDefault == parameterSig.HasDefault;
    }

    public bool Equals(IParameterSymbol? parameterSymbol)
    {
        return parameterSymbol is not null &&
            string.Equals(this.Name, parameterSymbol.Name) &&
            this.Type.Equals(parameterSymbol.Type) &&
            this.IsParams == parameterSymbol.IsParams &&
            this.HasDefault == parameterSymbol.HasExplicitDefaultValue;
    }

    public bool Equals(ParameterInfo? parameterInfo)
    {
        return parameterInfo is not null &&
            string.Equals(this.Name, parameterInfo.Name) &&
            this.Type.Equals(parameterInfo.ParameterType) &&
            this.IsParams == (parameterInfo.GetCustomAttribute<ParamArrayAttribute>() != null) &&
            this.HasDefault == parameterInfo.HasDefaultValue;
    }

    public override bool Equals(object? obj)
    {
        if (obj is ParameterSig parameterSig) return Equals(parameterSig);
        if (obj is IParameterSymbol parameterSymbol) return Equals(parameterSymbol);
        if (obj is ParameterInfo parameterInfo) return Equals(parameterInfo);
        return false;
    }

    public override int GetHashCode()
    {
        return Hasher.Create(Name, Type, IsParams, HasDefault);
    }

    public override string ToString()
    {
        return $"{(IsParams ? "params " : "")}{Type} {Name}{(HasDefault ? " = " : "")}{(HasDefault ? Default : null)}";
    }
}

public class MemberSig : IEquatable<MemberSig>, IEquatable<ISymbol>, IEquatable<MemberInfo>
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
    public static MemberSig FromMemberInfo(MemberInfo memberInfo)
    {
        throw new NotImplementedException();
    }

    public static MemberSig FromImplementation(
        Visibility visibility,
        Instic instic,
        MemberKeywords memberKeywords,
        string name,
        MemberType memberType,
        TypeSig returnType,
        ImmutableArray<ParameterSig> paramTypes)
    {
        return new MemberSig
        {
            Visibility = visibility,
            Instic = instic,
            Keywords = memberKeywords,
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
    public TypeSig ReturnType { get; init; }
    public ImmutableArray<ParameterSig> ParamTypes { get; init; }

    // ctors

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
            this.ReturnType.Equals(memberSig.ReturnType))
        {
            var attrComp = new EnumerableEqualityComparer<AttributeData>(
                new FuncEqualityComparer<AttributeData>(
                    (x, y) => x.GetFullTypeName() == y.GetFullTypeName(),
                    k => k.GetFullTypeName()?.GetHashCode() ?? 0));
            if (!attrComp.Equals(this.Attributes, memberSig.Attributes)) return false;
            var paramComp = EnumerableEqualityComparer<ParameterSig>.Default;
            if (!paramComp.Equals(this.ParamTypes, memberSig.ParamTypes)) return false;
            return true;
        }
        return false;
    }

    public bool Equals(ISymbol? symbol)
    {
        return symbol is not null && Equals(FromSymbol(symbol));
    }

    public bool Equals(MemberInfo? memberInfo)
    {
        return memberInfo is not null && Equals(FromMemberInfo(memberInfo));
    }

    public override bool Equals(object obj)
    {
        if (obj is MemberSig memberSig) return Equals(memberSig);
        if (obj is ISymbol symbol) return Equals(symbol);
        if (obj is MemberInfo memberInfo) return Equals(memberInfo);
        return false;
    }

    public override int GetHashCode()
    {
        var hasher = new Hasher();
        hasher.Add(Visibility);
        hasher.Add(Instic);
        hasher.Add(Keywords);
        hasher.Add(Name);
        hasher.Add(MemberType);
        hasher.Add(ReturnType);
        hasher.AddAll(ParamTypes);
        return hasher.ToHashCode();
    }
}


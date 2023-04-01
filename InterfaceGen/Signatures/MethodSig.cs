namespace Jay.SourceGen.InterfaceGen;

public class MethodSig : SymbolSig
{
    public MethodSig(IMethodSymbol methodSymbol)
        : base(methodSymbol)
    {
        if (methodSymbol.MethodKind is MethodKind.Constructor or MethodKind.StaticConstructor)
        {
            this.MemberType = MemberType.Constructor;
        }
        else
        {
            this.MemberType = MemberType.Method;
        }
        this.ReturnType = methodSymbol.ReturnType;
        this.ParamTypes = methodSymbol.Parameters;
    }
}


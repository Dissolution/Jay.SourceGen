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


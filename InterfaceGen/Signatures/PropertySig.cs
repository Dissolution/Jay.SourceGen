namespace Jay.SourceGen.InterfaceGen;

public class PropertySig : SymbolSig
{
    public string GetStr { get; }
    public string SetStr { get; }

    public PropertySig(IPropertySymbol propertySymbol)
        : base(propertySymbol)
    {
        this.MemberType = MemberType.Property;
        this.ReturnType = propertySymbol.Type;
        this.ParamTypes = propertySymbol.Parameters;
        // Getter
        var getMethod = propertySymbol.GetMethod;
        if (getMethod is null)
        {
            this.GetStr = "";
        }
        else
        {
            this.GetStr = " get;";
        }
        // Setter
        var setMethod = propertySymbol.SetMethod;
        if (setMethod is null)
        {
            this.SetStr = "";
        }
        else
        {
            if (setMethod.IsInitOnly)
            {
                this.SetStr = " init;";
            }
            else
            {
                this.SetStr = " set;";
            }
        }
    }

    public override void WriteDeclaration(CodeBuilder codeBuilder)
    {
        this.Visibility.DeclareTo(codeBuilder);
        this.Instic.DeclareTo(codeBuilder);
        this.Keywords.DeclareTo(codeBuilder);
        codeBuilder.Value(ReturnType).Append(' ')
            .Append(Name)
            .Append(" {")
            .Append(GetStr)
            .Append(SetStr)
            .Append(" }");
    }
}


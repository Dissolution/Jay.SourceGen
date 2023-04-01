using System.Collections.Immutable;

namespace Jay.SourceGen.InterfaceGen;

public class PropertySig : SymbolSig
{
    public bool HasGet { get; } = false;
    public bool HasInit { get; } = false;
    public bool HasSet { get; } = false;

    public PropertySig(IPropertySymbol propertySymbol)
        : base(propertySymbol)
    {
        this.MemberType = MemberType.Property;
        this.ReturnType = new(propertySymbol.Type);
        this.ParamTypes = propertySymbol.Parameters.Select(p => new ParameterSig(p)).ToImmutableArray();
        // Getter
        var getMethod = propertySymbol.GetMethod;
        this.HasGet = getMethod is not null;
        // Setter
        var setMethod = propertySymbol.SetMethod;
        if (setMethod is not null)
        {
            if (setMethod.IsInitOnly)
            {
                this.HasInit = true;
            }
            else
            {
                this.HasSet = true;
            }
        }
    }

    public string FieldName()
    {
        string propertyName = this.Name;
        if (string.IsNullOrEmpty(propertyName))
            throw new InvalidOperationException();
        int p = 0;

        Span<char> name = stackalloc char[propertyName.Length + 1];
        int n = 0;
        name[n++] = '_';
        name[n++] = char.ToLower(propertyName[p++]);
        TextHelper.CopyTo(propertyName[p..], name[n..]);
        return name.ToString();
    }

   
}


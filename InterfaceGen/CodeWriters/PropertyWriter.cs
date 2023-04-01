using System.ComponentModel;

namespace Jay.SourceGen.InterfaceGen.CodeWriters;

internal sealed class PropertyWriter : SectionWriter
{
    public PropertyWriter()
    {
        this.AddSectionWrite(Instic.Instance, Visibility.Public, MemberType.Property, WriteProperties);
        this.AddSectionWrite(Instic.Instance, Visibility.Public, MemberType.Constructor, WriteConstructors);
    }

    public override bool CanImplement(INamedTypeSymbol interfaceSymbol)
    {
        return false; // only manually added
    }

    private static void WriteProperties(CodeBuilder codeBuilder, GenerateInfo generate)
    {
        // Properties
        var properties = generate
            .Members
            .OfType<PropertySig>()
            .ToList();

        codeBuilder.LineDelimit(properties, static (cb, p) => p.WriteDeclaration(cb));
    }

    private static void WriteConstructors(CodeBuilder codeBuilder, GenerateInfo generate)
    {
        generate.GetLocals(out var type, out var varName);

        codeBuilder.CodeLine($"public {type}() {{ }}").NewLine();

        // Properties
        var properties = generate
            .Members
            .OfType<PropertySig>()
            .ToList();

        if (properties.Count > 0)
        {
            codeBuilder.Code($"public {type}(")
                .Delimit(", ", properties, static (cb, p) => cb.Code($"{p.ReturnType} {p.VarName}"))
                .AppendLine(')')
                .BracketBlock(ctorBlock => ctorBlock.LineDelimit(properties, static (cb, p) => cb.Code($"this.{p.Name} = {p.VarName};")));
        }

    }
}

public class NotifyPropertyWriter : SectionWriter
{
    public NotifyPropertyWriter()
    {
        this.AddSectionWrite(Instic.Instance, Visibility.Public, MemberType.Event, WriteEvents);
        this.AddSectionWrite(Instic.Instance, Visibility.Public, MemberType.Property, WriteProperties);
        this.AddSectionWrite(Instic.Instance, Visibility.Public, MemberType.Method, WriteMethods);
    }

    public override IEnumerable<string> GetNeededUsings()
    {
        yield return "System.ComponentModel";
    }

    public override bool CanImplement(INamedTypeSymbol interfaceSymbol)
    {
        return interfaceSymbol.IsType<INotifyPropertyChanged>() ||
            interfaceSymbol.IsType<INotifyPropertyChanging>();
    }

    private static void WriteEvents(CodeBuilder code, GenerateInfo generate)
    {
        generate.Members.Add(MemberSig.FromImplementation(
            Visibility.Public,
            Instic.Instance,
            MemberKeywords.None,
            "PropertyChanged",
                        MemberType.Event,


        if (generate.HasInterface<INotifyPropertyChanged>())
        {
            code.CodeBlock($$"""
                /// <inheritdoc cref="INotifyPropertyChanged"/>
                public event PropertyChangedEventHandler? PropertyChanged;
                """);
            generate.Members.Add(MemberSig.Get)
        }
         if (generate.HasInterface<INotifyPropertyChanging>())
        {
            code.CodeBlock($$"""
                /// <inheritdoc cref="INotifyPropertyChanging"/>
                public event PropertyChangingEventHandler? PropertyChanging;
                """);
        }
    }

    private static void WriteProperties(CodeBuilder code, GenerateInfo generate)
    {

    }
    private static void WriteMethods(CodeBuilder code, GenerateInfo generate)
    {

    }
}

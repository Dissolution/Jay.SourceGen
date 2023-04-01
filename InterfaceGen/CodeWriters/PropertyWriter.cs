using Jay.SourceGen.Coding;

using System.Collections.Immutable;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Xml.Linq;

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

        codeBuilder.LineDelimit(properties, static (cb, p) =>
        {
            p.Visibility.DeclareTo(cb);
            p.Instic.DeclareTo(cb);
            p.Keywords.DeclareTo(cb);
            cb.Code($"{p.ReturnType} {p.Name} {{");
            if (p.HasGet)
                cb.Append(" get;");
            if (p.HasInit)
                cb.Append(" init;");
            else if (p.HasSet)
                cb.Append(" set;");
            cb.Append(" }");
        });
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
        yield return "System.Runtime.CompilerServices";
    }

    public override bool CanImplement(INamedTypeSymbol interfaceSymbol)
    {
        return interfaceSymbol.IsType<INotifyPropertyChanged>() ||
            interfaceSymbol.IsType<INotifyPropertyChanging>();
    }

    private static void WriteEvents(CodeBuilder code, GenerateInfo generate)
    {
        if (generate.HasInterface<INotifyPropertyChanged>())
        {
            if (generate.Members.Add(MemberSig.FromImplementation(
                Visibility.Public,
                Instic.Instance,
                MemberKeywords.None,
                "PropertyChanged",
                MemberType.Event,
                typeof(PropertyChangingEventHandler),
                ImmutableArray<ParameterSig>.Empty)))
            {
                code.CodeBlock($$"""
                    /// <inheritdoc cref="INotifyPropertyChanged"/>
                    public event PropertyChangedEventHandler? PropertyChanged;
                    """);
            }
        }
        if (generate.HasInterface<INotifyPropertyChanging>())
        {
            if (generate.Members.Add(MemberSig.FromImplementation(
                Visibility.Public,
                Instic.Instance,
                MemberKeywords.None,
                "PropertyChanging",
                MemberType.Event,
                typeof(PropertyChangingEventHandler),
                ImmutableArray<ParameterSig>.Empty)))
            {
                code.CodeBlock($$"""
                    /// <inheritdoc cref="INotifyPropertyChanging"/>
                    public event PropertyChangingEventHandler? PropertyChanging;
                    """);
            }
        }
    }

   


    private static void WriteFields(CodeBuilder code, GenerateInfo generate)
    {
        // Properties
        var properties = generate.Members.OfType<PropertySig>().ToList();
        // To backing Fields
        code.LineDelimit(properties, static (propBuilder, propSig) => propBuilder.Code($"private {propSig.ReturnType} {propSig.FieldName()};"));
    }

    private static void WriteProperties(CodeBuilder code, GenerateInfo generate)
    {
        // Properties
        var properties = generate.Members.OfType<PropertySig>().ToList();

        code.LineDelimit(properties, (propBuilder, propSig) =>
        {
            propBuilder.CodeLine($"public {propSig.ReturnType} {propSig.Name}")
            .BracketBlock(propBlock =>
            {
                var fieldName = propSig.FieldName();

                if (propSig.HasGet)
                {
                    propBlock.CodeLine($"get => this.{fieldName};");
                }
                if (propSig.HasInit || propSig.HasSet)
                {
                    propBlock.CodeLine($"{(propSig.HasInit ? "init" : "set")} => this.SetField<{propSig.ReturnType}>(ref {fieldName}, value);";
                }
            });
        });
    }


    private static void WriteMethods(CodeBuilder code, GenerateInfo generate)
    {
        string keywords;
        if (generate.MemberKeywords.HasFlag(MemberKeywords.Sealed))
            keywords = "private";
        else
            keywords = "protected";

        bool isChanging = generate.HasInterface<INotifyPropertyChanging>();
        bool isChanged = generate.HasInterface<INotifyPropertyChanged>();

        if (isChanging)
        {
            code.CodeBlock($$"""
                {{keywords}} void OnPropertyChanging([CallerMemberName] string? propertyName = null)
                {
                    if (propertyName is not null)
                    {
                        this.PropertyChanging?.Invoke(this, new PropertyChangingEventArgs(propertyName));
                    }
                }
                """);
        }
        if (isChanged)
        {
            code.CodeBlock($$"""
                {{keywords}} void OnPropertyChanged([CallerMemberName] string? propertyName = null)
                {
                    if (propertyName is not null)
                    {
                        this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
                    }
                }
                """);
        }

        code.CodeBlock($$"""
            {{keywords}} bool SetField<T>(ref T field, T newValue, bool force = false, [CallerMemberName] string? propertyName = null)
            {
                if (force || !EqualityComparer<T>.Default.Equals(field, newValue))
                {
                    {{(isChanging ? "this.OnPropertyChanging(propertyName);" : "")}}
                    field = newValue;
                    {{(isChanged ? "this.OnPropertyChanged(propertyName);" : "")}}
                }
            }
            """);
    }
}

using Jay.SourceGen.Extensions;
using Jay.SourceGen.Text;

namespace Jay.SourceGen.InterfaceGen.CodeWriters;

internal sealed class ToStringImplWriter : SectionWriter
{
    public ToStringImplWriter()
    {
        AddSectionWrite(Instic.Instance, Visibility.Public, MemberType.Method,
            WriteInstanceMethods);
    }

    public override bool CanImplement(INamedTypeSymbol interfaceSymbol)
    {
        return true;
    }

    private static void WriteInstanceMethods(CodeBuilder codeBuilder, GenerateInfo generate)
    {
        if (generate.HasMember(Instic.Instance, Visibility.Public, MemberType.Method,
            "ToString",
            rt => rt.Name == "System.String",
            pt => pt.IsDefaultOrEmpty))
        {
            // Do not overwrite another ToString()
            return;
        }

        // Display members
        var displayMembers = generate.Members
            .Where(m => m.Attributes.Any(attr => attr.AttributeClass?.GetFQN() == Code.DisplayAttributeFQN))
            .ToList();

        codeBuilder.Append("public")
            .AppendIf(generate.MemberKeywords.HasFlag(MemberKeywords.Sealed), " ", " virtual ")
            .Append("string ToString()")
            .BracketBlock(methodBlock =>
            {
                if (displayMembers.Count == 0)
                {
                    methodBlock.CodeLine($"return \"{generate.Name}\";");
                }
                else
                {
                    methodBlock.AppendLine("return $$\"\"\"")
                    .IndentBlock("\t", ib =>
                    {
                        ib.AppendLine(generate.Name)
                        .BracketBlock(nameBlock =>
                        {
                            ib.Delimit(static cb => cb.NewLine(), displayMembers,
                                (cb, dm) => cb.Append(dm.Name).Append(" = {{").Append(dm.Name).Append("}},"));
                        })
                        .Append("\"\"\";");
                    });
                }
            });
    }
}

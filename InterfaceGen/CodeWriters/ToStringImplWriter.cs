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
        return false; // only manually added
    }

    private static IReadOnlyList<MemberSig> GetDisplayProperties(GenerateInfo generate)
    {
        // Filter only properties
        var properties = generate.Members.Where(m => m.MemberType == MemberType.Property).ToList();

        // Check for [Display]
        var displays = properties.Where(p => p.HasAttribute(Code.DisplayAttributeFQN)).ToList();
        // if there are none, just use properties
        if (displays.Count == 0)
        {
            return properties;
        }
        return displays;
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
        IReadOnlyList<MemberSig> displayMembers = GetDisplayProperties(generate);

        codeBuilder.AppendLine("public override string ToString()")
            .BracketBlock(methodBlock =>
            {
                if (displayMembers.Count == 0)
                {
                    methodBlock.CodeLine($"return \"{generate.ImplementationTypeName}\";");
                }
                else
                {
                    methodBlock.AppendLine("return $$\"\"\"")
                    .IndentBlock(ib =>
                    {
                        ib.AppendLine(generate.ImplementationTypeName)
                        .BracketBlock(nameBlock =>
                        {
                            ib.Delimit(static cb => cb.NewLine(), displayMembers,
                                (cb, dm) => cb.Append(dm.Name).Append(" = {{this.").Append(dm.Name).Append("}},"));
                        }).NewLine()
                        .Append("\"\"\";");
                    });
                }
            });
    }
}

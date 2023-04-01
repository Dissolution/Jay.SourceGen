namespace Jay.SourceGen.InterfaceGen.CodeWriters;

public sealed class FormattableWriter : SectionWriter
{
    public FormattableWriter()
    {
        this.AddSectionWrite(Instic.Instance, Visibility.Public, MemberType.Method, WriteSection);
    }

    public override bool CanImplement(INamedTypeSymbol interfaceSymbol)
    {
        return interfaceSymbol.GetFQN() == typeof(IFormattable).FullName;
    }

    private static void WriteSection(CodeBuilder codeBuilder, GenerateInfo generate)
    {
         if (generate.HasMember(Instic.Instance, Visibility.Public, MemberType.Method,
            "ToString",
            rt => rt.Name == "System.String",
            pt => pt.Length == 2)) // TODO: more checks
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
            .Append("string ToString(string? format, IFormatProvider? provider = null)")
            .BracketBlock(methodBlock =>
            {
                /* We support some cool custom formats
                 * aA - Display _all_ Members
                 * ??? TODO
                 * _ - always defaults to ToString() if we have no format
                 */
                methodBlock.CodeBlock($$"""
                    if (string.IsNullOrEmpty(format))
                        return this.ToString();
                    if (format.Length != 1)
                        throw new ArgumentException("Invalid format", nameof(format));
                    char f = format[0];
                    if (f == 'a' || f == 'A')
                    {
                        throw new NotImplementedException();
                    }
                    // else others
                     
                    throw new ArgumentException("Invalid format", nameof(format));
                    """);
            });
    }

}

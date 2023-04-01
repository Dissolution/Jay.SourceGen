namespace Jay.SourceGen.InterfaceGen.CodeWriters;

public sealed class ComparableWriter : SectionWriter
{
    private static readonly string _icomparableNameStart = $"{typeof(IComparable<>).FullName[..^2]}<";

    public ComparableWriter()
    {
        this.AddSectionWrite(Instic.Static, Visibility.Public, MemberType.Operator, WriteOperators);
        this.AddSectionWrite(Instic.Instance, Visibility.Public, MemberType.Method, WriteMethods);
    }

    public override bool CanImplement(INamedTypeSymbol interfaceSymbol)
    {
        var ifqn = interfaceSymbol.ToString();
        var implements = ifqn.StartsWith(_icomparableNameStart) && ifqn.EndsWith(">");
        return implements;
    }

    private static void WriteOperators(CodeBuilder codeBuilder, GenerateInfo generate)
    {
        var type = generate.ImplementationTypeName;

        // Account for null?
        if (generate.ObjType == ObjType.Class)
        {
            codeBuilder.CodeBlock($$"""
                public static bool operator <({{type}}? left, {{type}}? right)
                {
                    if (left is null)
                    {
                        return right is not null;   // `null` is only less than non-`null`
                    }
                    if (right is null)
                    {
                        return false;   // nothing is smaller than `null`
                    }
                    return left.CompareTo(right) < 0;
                }
                public static bool operator <=({{type}}? left, {{type}}? right)
                {
                    if (left is null)
                    {
                        return true;    // `null` is less than or equal to everything, including another `null`
                    }
                    if (right is null)
                    {
                        return false;   // (left is null)
                    }
                    return left.CompareTo(right) <= 0;
                }
                public static bool operator >({{type}}? left, {{type}}? right)
                {
                    if (left is null)
                    {
                        return false;   // `null` is greater than nothing
                    }
                    if (right is null)
                    {
                        return true;    // (left is not null)
                    }
                    return left.CompareTo(right) > 0;
                }
                public static bool operator >=({{type}}? left, {{type}}? right)
                {
                    if (left is null)
                    {
                        return right is null;   // `null` is only greater than or equal to another `null`
                    }
                    if (right is null)
                    {
                        return true;   // everything is greater than or equal to `null`
                    }
                    return left.CompareTo(right) >= 0;
                }
                """);
        }
        else
        {
            codeBuilder.CodeBlock($$"""
                public static bool operator <({{type}} left, {{type}} right)
                {
                    return left.CompareTo(right) < 0;
                }
                public static bool operator <=({{type}} left, {{type}} right)
                {
                    return left.CompareTo(right) <= 0;
                }
                public static bool operator >({{type}} left, {{type}} right)
                {
                    return left.CompareTo(right) > 0;
                }
                public static bool operator >=({{type}} left, {{type}} right)
                {
                    return left.CompareTo(right) >= 0;
                }
                """);
        }
    }

    private static void WriteMethods(CodeBuilder codeBuilder, GenerateInfo generate)
    {
        generate.GetLocals(out var typeName, out var varName);
        var interfaceType = generate.InterfaceTypeSymbol.Name;
        var interfaceVarName = generate.InterfaceTypeSymbol.GetVariableName();

        // Check for [Key] properties
        IReadOnlyList<MemberSig> keyProperties = generate.MembersWithAttribute(Code.KeyAttributeFQN).ToList();
        // if there are none, then we're a record
        if (keyProperties.Count == 0)
        {
            keyProperties = generate.Members.Where(m => m.MemberType == MemberType.Property).ToList();
        }
        if (keyProperties.Count == 0)
        {
            // We have no way of doing equality?
            throw new InvalidOperationException();
        }

        // We always get the interface compare
        codeBuilder
            .CodeBlock($$"""
                 public int CompareTo({{interfaceType}}? {{interfaceVarName}})
                 {
                     // We exist, so we're 'bigger' than null
                     if ({{interfaceVarName}} is null) return 1;
                     int c;
                     {{(CBA)(cb => compareProperties(cb, interfaceVarName))}}
                     // we know c == 0
                     return 0;
                 }
                 """)
            .NewLines(2);

        // For structs, we also generate a direct compare to avoid boxing
        if (generate.ObjType == ObjType.Struct)
        {
            codeBuilder
                .CodeBlock($$"""
                    public int CompareTo({{typeName}} {{varName}})
                    {
                        int c;
                        {{(CBA)(cb => compareProperties(cb, varName))}}
                        // we know c == 0
                        return 0;
                    }
                    """)
                .NewLines(2); 
        }


        void compareProperties(CodeBuilder code, string varName)
        {
            code.Enumerate(keyProperties, (pb, p) => pb
            .CodeBlock($"""
                c = Comparer<{p.ReturnType}>.Default.Compare(this.{p.Name}, {varName}.{p.Name});
                if (c != 0) return c;
                """)
            .NewLine());
        }
    }
}

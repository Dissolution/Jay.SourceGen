namespace Jay.SourceGen.InterfaceGen.CodeWriters;

public sealed class EquatableWriter : SectionWriter
{
    private static readonly string _iequatableNameStart = $"{typeof(IEquatable<>).FullName[..^2]}<";
    private const int HASH_SEED = 486_187_739;
    private const int HASH_MUL = 31;

    public EquatableWriter()
    {
        this.AddSectionWrite(Instic.Static, Visibility.Public, MemberType.Operator, WriteOperators);
        this.AddSectionWrite(Instic.Instance, Visibility.Public, MemberType.Method, WriteMethods);
    }

    public override bool CanImplement(INamedTypeSymbol interfaceSymbol)
    {
        var ifqn = interfaceSymbol.ToString();
        var implements = ifqn.StartsWith(_iequatableNameStart) && ifqn.EndsWith(">");
        return implements;
    }

    private static void WriteOperators(CodeBuilder codeBuilder, GenerateInfo generate)
    {
        var type = generate.ImplementationTypeName;

        // Account for null?
        if (generate.ObjType == ObjType.Class)
        {
            codeBuilder.CodeBlock($$"""
                public static bool operator ==({{type}}? left, {{type}}? right)
                {
                    if (ReferenceEquals(left, right)) return true;
                    if (left is null) return (right is null);
                    return left.Equals(right);
                }
                public static bool operator !=({{type}}? left, {{type}}? right)
                {
                    if (ReferenceEquals(left, right)) return false;
                    if (left is null) return (right is not null);
                    return !left.Equals(right);
                }
                """);
        }
        else
        {
            codeBuilder.CodeBlock($$"""
                public static bool operator ==({{type}} left, {{type}} right)
                {
                    return left.Equals(right);
                }
                public static bool operator !=({{type}} left, {{type}} right)
                {
                    return !left.Equals(right);
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

        // Account for null?
        if (generate.ObjType == ObjType.Class)
        {
            codeBuilder
                .CodeLine($"public bool Equals({interfaceType}? {interfaceVarName})")
                .BracketBlock(methodBlock =>
                {
                    methodBlock.AppendLine($"if ({interfaceVarName} is null) return false;");
                    checkProperties(methodBlock, interfaceVarName);
                })
                .NewLines(2)
                .CodeLine($"public override bool Equals(object? obj)")
                .BracketBlock(methodBlock =>
                {
                    methodBlock.CodeLine($"return obj is {interfaceType} {interfaceVarName} && Equals({interfaceVarName});");
                })
                .NewLines(2);
        }
        else
        {
            codeBuilder
                .AppendLine($"public bool Equals({interfaceType} {interfaceVarName})")
                .BracketBlock(methodBlock =>
                {
                    checkProperties(methodBlock, interfaceVarName);
                }).NewLines(2)
                // Also for our direct type (non-interface, avoids boxing)
                .AppendLine($"public bool Equals({typeName} {varName})")
                .BracketBlock(methodBlock =>
                {
                    checkProperties(methodBlock, varName);
                }).NewLines(2)
                .CodeLine($"public override bool Equals(object? obj)")
                .BracketBlock(methodBlock =>
                {
                    methodBlock.CodeBlock($$"""
                        if (obj is {{typeName}} {{varName}}) return Equals({{varName}});
                        if (obj is {{interfaceType}} {{interfaceVarName}}) return Equals({{interfaceVarName}});
                        """);
                }).NewLines(2);
        }


        codeBuilder.AppendLine("public override int GetHashCode()")
            .BracketBlock(methodBlock =>
            {
                methodBlock.CodeLine($"int hash = {HASH_SEED};")
                    .Enumerate(keyProperties, static (cb, kp) =>
                    {
                        if (kp.ReturnType.IsType<int>())
                        {
                            cb.CodeLine($"hash = (hash * {HASH_MUL}) + this.{kp.Name};");
                        }
                        else if (kp.ReturnType.CanBeNull())
                        {
                            cb.CodeBlock($$"""
                                if (this.{{kp.Name}} is null)
                                {
                                    hash *= {{HASH_MUL}};
                                }
                                else
                                {
                                    hash = (hash * {{HASH_MUL}}) + this.{{kp.Name}}.GetHashCode();
                                }
                                """).NewLine();
                        }
                        else
                        {
                            cb.CodeLine($"hash = (hash * {HASH_MUL}) + this.{kp.Name}.GetHashCode();");
                        }
                    })
                    .AppendLine("return hash;");
            });


        void checkProperties(CodeBuilder methodBlock, string varName)
        {
            methodBlock.Append("return ")
                .Delimit(static b => b.AppendLine(" && "),
                keyProperties,
                (b, p) =>
            {
                b.Code($"EqualityComparer<{p.ReturnType}>.Default.Equals(this.{p.Name}, {varName}.{p.Name})");
            }).AppendLine(";");
        }

    }


}

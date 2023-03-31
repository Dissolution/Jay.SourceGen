using System.Collections.Immutable;
using System.Diagnostics;
using Jay.SourceGen.EnumGen.Attributes;
using Jay.SourceGen.Extensions;
using Jay.SourceGen.Text;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Jay.SourceGen.EnumGen;

[Generator]
public sealed class EnumImplGenerator : IIncrementalGenerator
{
    public string AttributeFQN => $"Jay.SourceGen.EnumGen.Attributes.{nameof(EnumAttribute)}";

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        // Add any post-init output files
        context.RegisterPostInitializationOutput(ctx =>
        {
            //foreach (var sourceCode in GetPostInitOutput())
            //{
            //    ctx.AddSource(sourceCode.FileName, sourceCode.Code);
            //}
        });

        // Initial filter for the attribute
        var typeDeclarations = context.SyntaxProvider
            .ForAttributeWithMetadataName(
                fullyQualifiedMetadataName: AttributeFQN,
                (syntaxNode, _) => syntaxNode is TypeDeclarationSyntax,
                (ctx, _) => (TypeDeclarationSyntax)ctx.TargetNode);

        // Combine with compilation
        var compilationAndDeclarations = context.CompilationProvider.Combine(typeDeclarations.Collect());

        // Send to processing
        context.RegisterSourceOutput(compilationAndDeclarations,
            (sourceContext, cads) => Process(cads.Left, sourceContext, cads.Right));
    }

    private void Process(Compilation compilation,
        SourceProductionContext sourceProductionContext,
        ImmutableArray<TypeDeclarationSyntax> typeDeclarations)
    {
        // If we have nothing to process, exit quickly
        if (typeDeclarations.IsDefaultOrEmpty) return;

#if ATTACH
            if (!Debugger.IsAttached)
            {
                Debugger.Launch();
            }
#endif

        // Get a passable CancellationToken
        var token = sourceProductionContext.CancellationToken;

        // Load our attribute's symbol
        INamedTypeSymbol? attributeSymbol = compilation
            .GetTypesByMetadataName(this.AttributeFQN)
            .FirstOrDefault();
        if (attributeSymbol is null)
        {
            // Cannot!
            throw new InvalidOperationException($"Could not load {nameof(INamedTypeSymbol)} for {AttributeFQN}");
        }

        // As per several examples, we need a distinct list or a grouping on SyntaxTree
        // I'm going with System.Text.Json's example

        foreach (var group in typeDeclarations.GroupBy(static sd => sd.SyntaxTree))
        {
            SyntaxTree syntaxTree = group.Key;
            SemanticModel semanticModel = compilation.GetSemanticModel(syntaxTree);
            CompilationUnitSyntax unitSyntax = (syntaxTree.GetRoot(token) as CompilationUnitSyntax)!;

            foreach (var typeDeclaration in group)
            {
                // Get the AttributeData
                INamedTypeSymbol? typeSymbol = semanticModel.GetDeclaredSymbol(typeDeclaration) as INamedTypeSymbol;
                if (typeSymbol is null)
                    continue;

                // Check if we have our attribute
                // Necessary????
                if (!typeSymbol.GetAttributes().Any(attr => string.Equals(attr.AttributeClass?.GetFQN(), AttributeFQN)))
                    continue;

                // We have a candidate
                var sourceCodes = ProcessType(typeDeclaration, typeSymbol);

                // Add whatever was produced
                foreach (var sourceCode in sourceCodes)
                {
                    sourceProductionContext.AddSource(sourceCode.FileName, sourceCode.Code);
                }
            }
        }
    }

    private IEnumerable<SourceCode> ProcessType(TypeDeclarationSyntax typeDeclarationSyntax, INamedTypeSymbol typeSymbol)
    {
        // has to be readonly struct
        if (!typeDeclarationSyntax.HasKeyword(SyntaxKind.ReadOnlyKeyword))
            yield break;
        if (typeDeclarationSyntax is not StructDeclarationSyntax)
            yield break;


        // Look at the struct
        var members = typeSymbol.GetMembers();

        // Did they define their own ToString?
        var hasToString = members
            .OfType<IMethodSymbol>()
            .Where(m => m.Name == nameof(ToString))
            .Where(m => m.Parameters.IsDefaultOrEmpty)
            .Any();

        // We're interested in public fields
        var publicFields = members
            .OfType<IFieldSymbol>()
            .Where(f => f.DeclaredAccessibility == Accessibility.Public)
            .ToList();

        // Some are enum members
        var enumMembers = publicFields
            .Where(f => f.Type.Equals(typeSymbol, SymbolEqualityComparer.Default))
            .Where(f => f.IsStatic)
            .Select(enumMember => enumMember.Name)
            .ToList();

        // Others are instance fields we need to respect
        var instanceFields = publicFields
            .Where(f => !f.Type.Equals(typeSymbol, SymbolEqualityComparer.Default))
            .Where(f => !f.IsStatic)
            .Select(instanceField => (instanceField.Name, instanceField.Type))
            .ToList();

        var attrData = new SymbolAttributeData(typeSymbol.GetAttributes());
        if (!attrData.TryGetAttributeArg(AttributeFQN, out var attrArgs))
            yield break;

        // We do not care if it passes or fails as 'false' is the default
        attrArgs.TryGetValue(nameof(EnumAttribute.Flags), out bool flags);

        EnumStructToGenerate tg = new()
        {
            Flags = flags,
            Type = typeSymbol,
            InstanceFields = instanceFields,
            EnumMembers = enumMembers,
            HasToString = hasToString,
        };

        var sourceCode = new EnumImplSourceCodeGenerator(tg).GetSourceCode();
        yield return sourceCode;
    }
}


public sealed class EnumImplSourceCodeGenerator
{
    private readonly EnumStructToGenerate _enumStructToGenerate;

    public ITypeSymbol Type => _enumStructToGenerate.Type;
    public string T => Type.Name;
    public string VarName => Type.GetVariableName();
    public IReadOnlyList<string> Members => _enumStructToGenerate.EnumMembers;

    public EnumImplSourceCodeGenerator(EnumStructToGenerate enumStructToGenerate)
    {
        _enumStructToGenerate = enumStructToGenerate;
    }


    internal void WriteMembersBacking(CodeBuilder codeBuilder)
    {
        var members = Members;
        var membersLen = members.Count;

        codeBuilder.CodeBlock($$"""
            private static readonly {{T}}[] __members;
            private static readonly string[] __memberNames;
            
            private static class Incrementer
            {
                public static int NextValue = {{(_enumStructToGenerate.SkipZero ? 0 : -1)}};
            }
            
            public static IReadOnlyList<string> MemberNames => __memberNames;
            public static IReadOnlyList<{{T}}> Members => __members;
            
            static {{T}}()
            {
                __members = new {{T}}[{{membersLen}}];
                __memberNames = new string[{{membersLen}}];
                {{(CBA)(cb => cb.Enumerate(members, static (mb, m, i) => mb
                    .CodeBlock($"""
                                __members[{i}] = {m};                                    
                                __memberNames[{i}] = "{m}";
                                """)))}}
            }
            
            public static string? GetName({{T}} {{VarName}})
            {
                TryGetName({{VarName}}, out string? name);
                return name;
            }
            
            public static bool TryGetName({{T}} {{VarName}}, out string? name)
            {
                int value = {{VarName}}.__value;
                if ((uint)value < {{membersLen}}U)
                {
                    name = __memberNames[value];
                    return true;
                }
                name = null;
                return false;
            }
            
            public static bool IsDefined({{T}} {{VarName}})
            {
                return ((uint){{VarName}}.__value) < {{membersLen}}U;
            }
            
            public static bool TryParse(ReadOnlySpan<char> text, 
                out {{T}} {{VarName}})
            {
                return TryParse(text, default, out {{VarName}});
            }

            public static bool TryParse(ReadOnlySpan<char> text, 
                IFormatProvider? provider, 
                out {{T}} {{VarName}})
            {
                // Check names
                {{(CBA)(cb => cb.Enumerate(members, (mb, m, i) => mb
                    .CodeBlock($$"""
                                if (text.Equals("{{m}}", StringComparison.OrdinalIgnoreCase))
                                {
                                    {{VarName}} = {{m}};
                                    return true;
                                }
                                """)))}}
                // int.TryParse?
                if (int.TryParse(text, provider, out int value))
                {
                    // parse the int value as a member
                    return TryParse(value, out {{VarName}});
                }
            
                // Failed to parse
                {{VarName}} = default;
                return false;
            }
                        
            public static bool TryParse(
                int value,
                out {{T}} {{VarName}})
            {
                if ((uint)value < {{membersLen}}U)
                {
                    {{VarName}} = __members[value];
                    return true;
                }
                {{VarName}} = default;
                return false;
            }
            
            public static bool TryParse(
                string? str,
                out {{T}} {{VarName}})
            {
                return TryParse(str.AsSpan(), default, out {{VarName}});
            }
            
            public static bool TryParse(
                string? str, 
                IFormatProvider? provider, 
                out {{T}} {{VarName}})
            {
                return TryParse(str.AsSpan(), provider, out {{VarName}});
            }

            public static {{T}} Parse(ReadOnlySpan<char> text, 
                IFormatProvider? provider = default)
            {
                if (!TryParse(text, provider, out var {{VarName}}))
                {
                    throw new ArgumentException($"Cannot parse '{(text.ToString())}' to a {{T}}", nameof(text));
                }
                return {{VarName}};
            }
            
            public static {{T}} Parse(string? str, 
                IFormatProvider? provider = default)
            {
                if (!TryParse(str, provider, out var {{VarName}}))
                {
                    throw new ArgumentException($"Cannot parse '{str}' to a {{T}}", nameof(str));
                }
                return {{VarName}};
            }

            """);
    }

    internal void WriteConstructors(CodeBuilder codeBuilder)
    {
        // If we have instance fields, we need to get their data in the constructor
        var instances = _enumStructToGenerate.InstanceFields;
        if (instances.Count == 0)
        {
            // We have to have a public, parameterless
            codeBuilder.CodeBlock($$"""
                public {{T}}()
                {
                    __value = Interlocked.Increment(ref Incrementer.NextValue);
                }
                """);
        }
        else
        {
            codeBuilder
                .Code($"private {T}(")
                .Delimit(", ", instances, static (cb, field) =>
                {
                    cb.Code($"{field.Type} {field.Name.ToVariableName()}");
                })
                .AppendLine(')')
                .BracketBlock(b =>
                {
                    b.Enumerate(instances, static (cb, field) =>
                        {
                            cb.CodeBlock($"this.{field.Name} = {field.Name.ToVariableName()};");
                        })
                        // We'll get the next available value
                        .AppendLine("__value = Interlocked.Increment(ref Incrementer.NextValue);");
                });
        }
    }

    internal SourceCode GetSourceCode()
    {
        using var codeBuilder = new CodeBuilder();

        codeBuilder.AutoGeneratedHeader()
            .Nullable(true)
            .NewLine()
            .Using("System")
            .Using("System.Runtime.CompilerServices")
            .Using("System.Diagnostics")
            .Using("System.Threading")
            .Directive("if", "NET7_0_OR_GREATER", d =>
            {
                d.Using("System.Numerics");
            })
            .NewLine()
            .Namespace(Type.GetNamespace())
            .CodeBlock($$"""
                    partial struct {{T}} :
                        IEquatable<{{T}}>,
                        IComparable<{{T}}>,
                        IFormattable
                    #if NET7_0_OR_GREATER
                        , IEqualityOperators<{{T}}, {{T}}, bool>
                        , IComparisonOperators<{{T}}, {{T}}, bool>
                        , IParsable<{{T}}>
                        , ISpanParsable<{{T}}>
                    #endif
                    #if NET6_0_OR_GREATER
                        , ISpanFormattable
                    #endif                    
                    {
                        [MethodImpl(MethodImplOptions.AggressiveInlining)]
                        public static bool operator ==({{T}} left, {{T}} right) => left.__value == right.__value;
                        [MethodImpl(MethodImplOptions.AggressiveInlining)]
                        public static bool operator !=({{T}} left, {{T}} right) => left.__value != right.__value;
                        [MethodImpl(MethodImplOptions.AggressiveInlining)]
                        public static bool operator >({{T}} left, {{T}} right) => left.__value > right.__value;
                        [MethodImpl(MethodImplOptions.AggressiveInlining)]
                        public static bool operator >=({{T}} left, {{T}} right) => left.__value >= right.__value;
                        [MethodImpl(MethodImplOptions.AggressiveInlining)]
                        public static bool operator <({{T}} left, {{T}} right) => left.__value < right.__value;
                        [MethodImpl(MethodImplOptions.AggressiveInlining)]
                        public static bool operator <=({{T}} left, {{T}} right) => left.__value <= right.__value;
                       
                        {{(CBA)WriteMembersBacking}}
                      

                        private readonly int __value;

                        {{(CBA)WriteConstructors}}

                        {{(CBA)(cb => cb.If(_enumStructToGenerate.Flags, b => b.Code($$"""
                            public partial {{T}} WithFlag({{T}} flag);
                            public partial {{T}} WithoutFlag({{T}} flag);

                            public bool HasFlag({{T}} flag)
                            {
                                return (__value & flag.__value) != 0;
                            }
                            """)))}}

                        public int CompareTo({{T}} {{VarName}})
                        {
                            return __value.CompareTo({{VarName}}.__value);
                        }

                        public bool Equals({{T}} {{VarName}})
                        {
                            return __value == {{VarName}}.__value;                            
                        }

                        public override bool Equals(object? obj)
                        {
                            if (obj is {{T}} {{VarName}})
                                return Equals({{VarName}});
                            return false;
                        }

                        public override int GetHashCode()
                        {
                            return __value;
                        }

                        /// <inheritdoc cref="ISpanFormattable"/>
                        public bool TryFormat(Span<char> destination, out int charsWritten, 
                            ReadOnlySpan<char> format = default, 
                            IFormatProvider? provider = default)
                        {
                            string str = ToString(format.ToString(), provider)!;
                            if (str.AsSpan().TryCopyTo(destination))
                            {
                                charsWritten = str.Length;
                                return true;
                            }
                            charsWritten = 0;
                            return false;
                        }

                        /// <inheritdoc cref="IFormattable"/>
                        public string ToString(string? format, IFormatProvider? formatProvider = default)
                        {
                            if (string.IsNullOrEmpty(format))
                                return ToString()!;
                            if (format.Length != 1)
                                return __value.ToString(format, formatProvider);
                            switch (format[0])
                            {
                                case 'G' or 'g':
                                {
                                    if (TryGetName(this, out var name))
                                    {
                                        return name;
                                    }
                                    return __value.ToString();
                                }
                                case 'X' or 'x':
                                {
                                    return __value.ToString("X");
                                }
                                case 'D' or 'd':
                                {
                                    return __value.ToString();
                                }
                                case 'F' or 'f':
                                {
                                    throw new NotImplementedException();
                                }
                                default:
                                {
                                    return __value.ToString(format, formatProvider);
                                }
                            }
                        }

                        {{(CBA)(cb => cb.If(!_enumStructToGenerate.HasToString, b => b.CodeLine("""
                                    public override string ToString()
                                    {
                                         if (TryGetName(this, out var name))
                                         {
                                             return name!;
                                         }
                                         return __value.ToString();             
                                    }
                                    """)))}}
                    }
                    """);

        string hintname = $"{Type.GetFQN()}.g.cs";
        string code = codeBuilder.ToString();

        Debugger.Break();

        return new(hintname, code);
    }
}
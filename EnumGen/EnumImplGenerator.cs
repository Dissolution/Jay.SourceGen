﻿using Jay.SourceGen.EnumGen.Attributes;
using Jay.SourceGen.Extensions;
using Jay.SourceGen.Text;

using Jaynums.SourceGen;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

using System.Diagnostics;

namespace Jay.SourceGen.EnumGen
{
    [Generator]
    public sealed class EnumImplGenerator : AttributeTypeDeclarationGenerator, IIncrementalGenerator
    {
        public override string AttributeFQN => $"Jay.SourceGen.EnumGen.Attributes.{nameof(EnumAttribute)}";

        protected override IEnumerable<SourceCode> ProcessType(TypeDeclarationSyntax typeDeclarationSyntax, INamedTypeSymbol typeSymbol)
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

            var sources = EnumCodeGenerator.GetSources(new[]{tg });
            foreach (var source in sources)
            {
                Debugger.Break();
                yield return new SourceCode(source.HintName, source.Code);
            }
        }
    }
}

internal class EnumCodeGenerator
{
    public EnumStructToGenerate EnumToGenerate { get; }
    public ITypeSymbol Type { get; }
    public string T { get; }
    public string VarName { get; }


    public EnumCodeGenerator(EnumStructToGenerate enumToGenerate)
    {
        this.EnumToGenerate = enumToGenerate;
        this.Type = enumToGenerate.Type;
        this.T = Type.Name;
        this.VarName = Type.GetVariableName();
    }

    protected virtual void WriteHeader(CodeBuilder codeBuilder)
    {
        codeBuilder
          .AutoGeneratedHeader()
          .Nullable(true)
          .NewLine()
          .Using("System")
          .Using("System.Runtime.CompilerServices")
          .Using("System.Threading")
          .Directive("if", "NET7_0_OR_GREATER", d => d
              .Using("System.Numerics"))
          .NewLine()
          .Namespace(Type.GetNamespace());
    }

    protected virtual void WriteStructDeclaration(CodeBuilder codeBuilder)
    {
        codeBuilder.Format($$"""
                readonly partial struct {{T}} :
                    IEquatable<{{T}}>,
                    IComparable<{{T}}>,
                    IFormattable
                #if NET6_0_OR_GREATER
                    , ISpanFormattable
                #endif
                #if NET7_0_OR_GREATER
                    , IEqualityOperators<{{T}}, {{T}}, bool>
                    , IComparisonOperators<{{T}}, {{T}}, bool>
                    , IParsable<{{T}}>
                    , ISpanParsable<{{T}}>
                #endif
                """);
    }

    protected virtual void WriteOperators(CodeBuilder codeBuilder)
    {
        codeBuilder.Format($$"""
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
                """);
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="codeBuilder"></param>
    protected virtual void WriteStaticBacking(CodeBuilder codeBuilder)
    {
        var enumMembers = EnumToGenerate.EnumMembers;
        var membersLength = EnumToGenerate.MembersLength;

        codeBuilder.Format($$"""
                private static readonly {{T}}[] __members;
                private static readonly string[] __memberNames;
                
                /// <summary>
                /// Get the names of all {{T}} members
                /// </summary>
                public static IReadOnlyList<string> Names => __memberNames;

                /// <summary>
                /// Get all {{T}} members
                /// </summary>
                public static IReadOnlyList<{{T}} Members => __members;
                """)
            .CodeLine($"static {T}()")
            .BracketBlock(ctorBuilder =>
            {
                ctorBuilder
                    .CodeLine($"__members = new {T}[{membersLength}];")
                    .CodeLine($"__memberNames = new string[{membersLength}];");
                int m;
                if (EnumToGenerate.SkipZero)
                {
                    ctorBuilder.CodeLine($$"""
                            __members[0] = default;
                            __memberNames[0] = string.Empty;
                            """);
                    m = 1;
                }
                else
                {
                    m = 0;
                }
                foreach (var enumMember in enumMembers)
                {
                    ctorBuilder.CodeLine($$"""
                            __members[{{m}}] = {{enumMember}};
                            __memberNames[{{m}}] = "{{enumMember}}";
                            """);
                    // Next m
                    m++;
                }
            })
            .NewLine()
            .CodeLine($$"""
                    public static string? GetName({{T}} {{VarName}})
                    {
                        TryGetName({{VarName}}, out string? name);
                        return name;
                    }
                    """)
            .NewLine()
            .CodeLine($"public static bool TryGetName({T} {VarName}, out string? name)")
            .BracketBlock(mb =>
            {
                if (EnumToGenerate.Flags)
                {
                    mb.Format($$"""
                            int value = {{VarName}}.__value;
                            if ((uint)value <= {{1U << enumMembers.Count}}U - 1U)
                            {
                                throw new NotImplementedException();              
                            }
                            name = null;
                            return false;
                            """);
                }
                else
                {
                    mb.Format($$"""
                            int value = {{VarName}}.__value;
                            if ((uint)value < {{membersLength}}U)
                            {
                                name = __memberNames[value];
                                return true;                               
                            }
                            name = null;
                            return false;
                            """);
                }
            })
            .CodeLine($"public static bool IsDefined({T} {VarName})")
            .BracketBlock(mb =>
            {
                if (EnumToGenerate.Flags)
                {
                    /* We automatically define T0 'None'
                     * Every defined member gets a bit / power of 2
                     * So, Member1 = 1 << 0, Member2 = 1 << 1,, etc.
                     * 1 << MemberCount is the next available bit
                     * Any value >= 0 and <= all flags is valid
                     * We can easily calculate all flags as ((1 << Member.Count) - 1)
                     */
                    mb.Code($$"""
                            return ((uint){{VarName}}.__value) <= ({{1U << enumMembers.Count}}U - 1U);
                            """);
                }
                else
                {
                    mb.Code($$"""
                            return ((uint){{VarName}}.__value) < {{membersLength}}U;
                            """);
                }
            })
            .NewLine();
    }


    public (string HintName, string Code) GenerateCode()
    {
        using var codeBuilder = new CodeBuilder();
        WriteHeader(codeBuilder);
        WriteStructDeclaration(codeBuilder);
        codeBuilder.BracketBlock(structBody =>
        {
            WriteOperators(structBody);
            WriteStaticBacking(structBody);
        });

        string hintname = $"{Type.GetFQN()}.g.cs";
        string code = codeBuilder.ToString();

        Debug.WriteLine(code);

        return (hintname, code);
    }

    internal static IEnumerable<(string HintName, string Code)> GetSources(IEnumerable<EnumStructToGenerate> toGenerate)
    {
        using var codeBuilder = new CodeBuilder();

        foreach (var enumToGenerate in toGenerate)
        {
            ITypeSymbol type = enumToGenerate.Type;
            string T = type.Name;
            var varName = type.GetVariableName();

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
                .Namespace(type.GetNamespace())
                .Code($$"""
                    partial struct {{T}} :
                        IEquatable<{{T}}>,
                        IComparable<{{T}}>,
                        IFormattable
                        {{(CBA)(cb => cb
                            .Directive("if", "NET7_0_OR_GREATER", b => b
                                .Code($$"""
                                    , IEqualityOperators<{{T}}, {{T}}, bool>
                                    , IComparisonOperators<{{T}}, {{T}}, bool>
                                    , IParsable<{{T}}>
                                    , ISpanParsable<{{T}}>
                                    """))
                            .Directive("if", "NET6_0_OR_GREATER", b => b
                                .Code($$"""
                                    , ISpanFormattable                            
                                    """)))}}
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
                       
                        private static readonly {{T}}[] __members;
                        private static readonly string[] __memberNames;

                        private static class Incrementer
                        {
                            public static int NextValue = {{(enumToGenerate.SkipZero ? 0 : -1)}};
                        }

                        public static IReadOnlyList<string> Names => __memberNames;
                        public static IReadOnlyList<{{T}} Members => __members;

                        static {{T}}()
                        {
                            __members = new {{T}}[{{enumToGenerate.MembersLength}}];
                            __memberNames = new string[{{enumToGenerate.MembersLength}}];
                            {{(CBA)(cb => cb.Enumerate(enumToGenerate.EnumMembers, (mb, m, i) => mb
                                .CodeLine($"""
                                    __members[{i}] = {m};                                    
                                    __memberNames[{i}] = "{m}";
                                    """)))}}
                        }

                        public static string? GetName({{T}} {{varName}})
                        {
                            TryGetName({{varName}}, out string? name);
                            return name;
                        }

                        public static bool TryGetName({{T}} {{varName}}, out string? name)
                        {
                            int value = {{varName}}.__value;
                            if ((uint)value >= {{enumToGenerate.MembersLength}}U)
                            {
                                name = null;
                                return false;
                            }
                            name = __memberNames[value];
                            return true;
                        }

                        public static bool IsDefined({{T}} {{varName}})
                        {
                            return ((uint){{varName}}.__value) < {{enumToGenerate.MembersLength}}U;
                        }

                        public static {{T}} Parse(
                            ReadOnlySpan<char> text, 
                            IFormatProvider? provider = default)
                        {
                            if (!TryParse(text, provider, out var {{varName}}))
                            {
                                throw new ArgumentException($"Cannot parse '{(text.ToString())}' to a {{T}}", nameof(text));
                            }
                            return {{varName}};
                        }

                        public static bool TryParse(ReadOnlySpan<char> text, 
                            IFormatProvider? provider, 
                            out {{T}} {{varName}})
                        {
                            // Check names
                            {{(CBA)(cb => cb.Enumerate(enumToGenerate.EnumMembers, (mb, m, i) => mb
                                .CodeLine($$"""
                                    if (text.Equals("{{m}}", StringComparison.OrdinalIgnoreCase))
                                    {
                                        {{varName}} = {{m}};
                                        return true;
                                    }
                                    """)))}}
                            // int.TryParse?
                            if (int.TryParse(text, provider, out int value))
                            {
                                return TryParse(value, out {{varName}});
                            }
                    
                            // Failed to parse
                            {{varName}} = default;
                            return false;
                        }

                        public static {{T}} Parse(
                            string? str, 
                            IFormatProvider? provider = default)
                        {
                            if (!TryParse(str, provider, out var {{varName}}))
                            {
                                throw new ArgumentException($"Cannot parse '{str}' to a {{T}}", nameof(str));
                            }
                            return {{varName}};
                        }

                        public static bool TryParse(
                            int value,
                            out {{T}} {{varName}})
                        {
                            if ((uint)value < {{enumToGenerate.MembersLength}}U)
                            {
                                {{varName}} = __members[value];
                                return true;
                            }
                            {{varName}} = default;
                            return false;
                        }

                        public static bool TryParse(
                            string? str,
                            out {{T}} {{varName}})
                        {
                            return TryParse(str, default, out {{varName}});
                        }

                        public static bool TryParse(
                            string? str, 
                            IFormatProvider? provider, 
                            out {{T}} {{varName}})
                        {
                            if (string.IsNullOrEmpty(str))
                            {
                                {{varName}} = default;
                                return false;
                            }
                            // Check names
                            {{(CBA)(cb => cb.Enumerate(enumToGenerate.EnumMembers, (mb, m, i) => mb
                                .CodeLine($$"""
                                    if (string.Equals(str, "{{m}}", StringComparison.OrdinalIgnoreCase))
                                    {
                                        {{varName}} = {{m}};
                                        return true;
                                    }
                                    """)))}}
                            // int.TryParse?
                            if (int.TryParse(str, provider, out int value))
                            {
                                return TryParse(value, out {{varName}});
                            }

                            // Failed to parse
                            {{varName}} = default;
                            return false;
                        }



                        private readonly int __value;

                        {{(CBA)(cb => EmitConstructors(cb, enumToGenerate))}}

                        {{(CBA)(cb => cb.If(enumToGenerate.Flags, b => b.Code($$"""
                            public partial {{T}} WithFlag({{T}} flag);
                            public partial {{T}} WithoutFlag({{T}} flag);

                            public bool HasFlag({{T}} flag)
                            {
                                return (__value & flag.__value) != 0;
                            }
                            """)))}}

                        public int CompareTo({{T}} {{varName}})
                        {
                            return __value.CompareTo({{varName}}.__value);
                        }

                        public bool Equals({{T}} {{varName}})
                        {
                            return __value == {{varName}}.__value;                            
                        }

                        public override bool Equals(object? obj)
                        {
                            if (obj is {{T}} {{varName}})
                                return Equals({{varName}});
                            return false;
                        }

                        public override int GetHashCode()
                        {
                            return __value;
                        }

                        public bool TryFormat(Span<char> destination, out int charsWritten, 
                            ReadOnlySpan<char> format = default, 
                            IFormatProvider? provider = default)
                        {
                            string str = ToString(format.ToString(), provider);
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
                                return this.ToString();
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

                        {{(CBA)(cb =>
                        {
                            if (!enumToGenerate.HasToString)
                            {
                                cb.Code("""
                                    public override string ToString()
                                    {
                                         if (TryGetName(this, out var name))
                                         {
                                             return name;
                                         }
                                         return __value.ToString();             
                                    }
                                    """);
                            }
                        })}}                        
                    }
                    """);

            string hintname = $"{enumToGenerate.Type.GetFQN()}.g.cs";
            string code = codeBuilder.ToString();

            Debug.WriteLine(code);

            Debugger.Break();

            yield return (hintname, code);
            codeBuilder.Clear();
        }
    }


    private static void EmitConstructors(CodeBuilder codeBuilder, EnumStructToGenerate toGenerate)
    {
        codeBuilder
            .Format($"private {toGenerate.Type}(")
            .Delimit(", ", toGenerate.InstanceFields, static (dcb, pif) =>
            {
                dcb.Code($"{pif.Type} {pif.Name.ToVariableName()}");
            })
            .AppendLine(')')
            .BracketBlock(bcb =>
            {
                bcb.Enumerate(toGenerate.InstanceFields, static (lcb, pif) =>
                {
                    lcb.CodeLine($"this.{pif.Name} = {pif.Name.ToVariableName()};");
                })
                // We'll get the next available value
                .AppendLine("__value = Interlocked.Increment(ref Incrementer.NextValue);");
            });
    }
}

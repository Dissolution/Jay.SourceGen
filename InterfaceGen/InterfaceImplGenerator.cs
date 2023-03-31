using Microsoft.CodeAnalysis.CSharp.Syntax;
using Jay.SourceGen.Extensions;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq.Expressions;
using System.Reflection;
using Jay.SourceGen.Text;
using Jay.SourceGen.Enums;
using System.Runtime.InteropServices.ComTypes;
using Jay.SourceGen.InterfaceGen.CodeWriters;

namespace Jay.SourceGen.InterfaceGen;

[Generator]
public class InterfaceImplGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        // Initial filter for the attribute
        var interfaceDeclarations = context
            .SyntaxProvider
            .ForAttributeWithMetadataName(
                fullyQualifiedMetadataName: Code.ImplementAttributeFQN,
                (syntaxNode, _) => syntaxNode is InterfaceDeclarationSyntax,
                (ctx, _) => (InterfaceDeclarationSyntax)ctx.TargetNode);

        // Combine with compilation
        var compilationAndDeclarations = context
            .CompilationProvider
            .Combine(interfaceDeclarations.Collect());

        // Send to processing
        context.RegisterSourceOutput(compilationAndDeclarations,
            (sourceContext, cads) => Process(cads.Left, sourceContext, cads.Right));

    }

    private void Process(Compilation compilation,
       SourceProductionContext sourceProductionContext,
       ImmutableArray<InterfaceDeclarationSyntax> interfaceDeclarations)
    {
        // If we have nothing to process, exit quickly
        if (interfaceDeclarations.IsDefaultOrEmpty) return;

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
            .GetTypesByMetadataName(Code.ImplementAttributeFQN)
            .FirstOrDefault();
        if (attributeSymbol is null)
        {
            // Cannot!
            throw new InvalidOperationException($"Could not load {nameof(INamedTypeSymbol)} for {Code.ImplementAttributeFQN}");
        }

        // As per several examples, we need a distinct list or a grouping on SyntaxTree
        // I'm going with System.Text.Json's example

        foreach (var group in interfaceDeclarations.GroupBy(static sd => sd.SyntaxTree))
        {
            SyntaxTree syntaxTree = group.Key;
            SemanticModel semanticModel = compilation.GetSemanticModel(syntaxTree);
            CompilationUnitSyntax unitSyntax = (syntaxTree.GetRoot(token) as CompilationUnitSyntax)!;

            foreach (var interfaceDeclaration in group)
            {
                // Interface's Type Symbol
                INamedTypeSymbol? typeSymbol = semanticModel.GetDeclaredSymbol(interfaceDeclaration) as INamedTypeSymbol;
                if (typeSymbol is null)
                    continue;
                // ImplementAttribute
                AttributeData? attrData = typeSymbol
                    .GetAttributes()
                    .FirstOrDefault(attr => string.Equals(attr.AttributeClass?.GetFQN(), Code.ImplementAttributeFQN));

                if (attrData is null)
                    continue;

                // Get our Generate Info
                GenerateInfo generateInfo = new()
                {
                    TypeSymbol = typeSymbol
                };

                // Data from the Attribute itself
                var args = attrData.GetArgs();
                if (args.TryGetValue<string>(nameof(ImplementAttribute.Name), out string? implementationName))
                {
                    generateInfo.Name = implementationName;
                }
                if (args.TryGetValue<bool>(nameof(ImplementAttribute.IsClass), out var isClass))
                {
                    generateInfo.ObjType = isClass ? ObjType.Class : ObjType.Struct;
                }
                if (args.TryGetValue<bool>(nameof(ImplementAttribute.IsPartial), out var isPartial))
                {
                    if (isPartial)
                    {
                        generateInfo.MemberKeywords |= MemberKeywords.Partial;
                    }
                    else
                    {
                        generateInfo.MemberKeywords &= ~MemberKeywords.Partial;
                    }
                }
                if (args.TryGetValue<bool>(nameof(ImplementAttribute.IsAbstract), out var isAbstract))
                {
                    if (isAbstract)
                    {
                        generateInfo.MemberKeywords |= MemberKeywords.Abstract;
                    }
                    else
                    {
                        generateInfo.MemberKeywords &= ~MemberKeywords.Abstract;
                    }
                }
                if (args.TryGetValue<bool>(nameof(ImplementAttribute.IsSealed), out var isSealed))
                {
                    if (isSealed)
                    {
                        generateInfo.MemberKeywords |= MemberKeywords.Sealed;
                    }
                    else
                    {
                        generateInfo.MemberKeywords &= ~MemberKeywords.Sealed;
                    }
                }

                // What interfaces are we implementing?
                generateInfo.Interfaces = typeSymbol.AllInterfaces;

                // Let's see what the interface declared
                var members = typeSymbol.GetMembers();
                foreach (var member in members)
                {
                    string name = member.Name;
                    ImmutableArray<AttributeData> attributes = member.GetAttributes();
                    Instic instic = member.IsStatic ? Instic.Static : Instic.Instance;
                    Visibility visibility = Visibility.Public; // HACK/TODO/FIXME
                    MemberType type;
                    ITypeSymbol returnType;
                    ImmutableArray<IParameterSymbol> paramTypes;

                    if (member is IFieldSymbol fieldSymbol)
                    {
                        type = MemberType.Field;
                        returnType = fieldSymbol.Type;
                        paramTypes = ImmutableArray<IParameterSymbol>.Empty;
                    }
                    else if (member is IPropertySymbol propertySymbol)
                    {
                        type = MemberType.Property;
                        returnType = propertySymbol.Type;
                        paramTypes = propertySymbol.Parameters;
                    }
                    else if (member is IEventSymbol eventSymbol)
                    {
                        type = MemberType.Event;
                        var thing = eventSymbol.Type;
                        Debugger.Break();
                        throw new NotImplementedException();
                    }
                    else if (member is IMethodSymbol methodSymbol)
                    {
                        switch (methodSymbol.MethodKind)
                        {
                            case MethodKind.Constructor:
                            case MethodKind.StaticConstructor:
                            {
                                type = MemberType.Constructor;
                                break;
                            }
                            default:
                            {
                                type = MemberType.Method;
                                break;
                            }
                        }
                        returnType = methodSymbol.ReturnType;
                        paramTypes = methodSymbol.Parameters;
                    }
                    else
                    {
                        Debugger.Break();
                        throw new NotImplementedException();
                    }

                    generateInfo.Members.Add(new(instic, visibility, type, attributes, name, returnType, paramTypes));
                }


                // We have a candidate
                var sourceCodes = ProcessType(generateInfo);

                // Add whatever was produced
                foreach (var sourceCode in sourceCodes)
                {
                    sourceProductionContext.AddSource(sourceCode.FileName, sourceCode.Code);
                }
            }
        }
    }

    private IEnumerable<SourceCode> ProcessType(GenerateInfo generateInfo)
    {
        if (string.IsNullOrWhiteSpace(generateInfo.Name))
        {
            generateInfo.Name = generateInfo.TypeSymbol.Name.Substring(1);
        }

        // Check the interfaces
        List<IInterfaceImplementationWriter> implWriters = new();

        var interfaceSymbols = generateInfo.Interfaces;
        if (!interfaceSymbols.IsDefaultOrEmpty)
        {
            foreach (var interfaceSymbol in interfaceSymbols)
            {
                var writer = InterfaceImplementationWriters.GetWriter(interfaceSymbol);
                if (writer is null)
                    throw new InvalidOperationException($"Cannot handle {interfaceSymbol}");
                implWriters.Add(writer);
            }
        }

        // Always add a possible ToString()
        implWriters.Add(new ToStringImplWriter());

        using var codeBuilder = new CodeBuilder()
            .AutoGeneratedHeader()
            .Nullable(true)
            // Usings?
            .NewLine()
            .Namespace(generateInfo.TypeSymbol.GetFQNamespace())
            // Implementation declaration
            .Enumerate(generateInfo.Visibility.GetFlags(), (cb, flag) => cb.Append(flag.ToString().ToLower()).Append(' '))
            .Enumerate(generateInfo.MemberKeywords.GetFlags(), (cb, flag) => cb.Append(flag.ToString().ToLower()).Append(' '))
            .Append(generateInfo.ObjType.ToString().ToLower()).Append(' ').Append(generateInfo.Name)
            // Interfaces
            .AppendLine(" : ")
            .IndentBlock(ib =>
            {
                // The main interface, always
                ib.Value(generateInfo.TypeSymbol);
                // Do we have others?
                if (!interfaceSymbols.IsDefaultOrEmpty)
                {
                    ib.AppendLine(',')
                       .Delimit(static b => b.AppendLine(','),
                           interfaceSymbols,
                           static (db, iface) => db.Value(iface));
                }
            })
            .BracketBlock(typeBlock =>
            {
                foreach (var instic in new[] { Instic.Static, Instic.Instance })
                    foreach (var memberType in new[] { MemberType.Operator, MemberType.Field, MemberType.Property, MemberType.Event, MemberType.Constructor, MemberType.Method })
                        foreach (var visibility in new[] { Visibility.Private, Visibility.Protected, Visibility.Internal, Visibility.Public })
                            foreach (var implWriter in implWriters)
                            {
                                if (implWriter.WriteImplementationSection(instic, visibility, memberType, typeBlock, generateInfo))
                                {
                                    typeBlock.NewLines(2);
                                }
                            }
            });

        string filename = $"{generateInfo.TypeSymbol.GetFQN()}.g.cs";
        string code = codeBuilder.ToString();

        Debugger.Break();

        SourceCode sourceCode = new SourceCode(filename, code);
        yield return sourceCode;
    }

}

using Microsoft.CodeAnalysis.CSharp.Syntax;

using System.Collections.Immutable;
using System.Diagnostics;

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

                // Data from the Attribute itself (constructor args + properties)
                var args = attrData.GetArgs();

                string? implementationName;
                if (!args.TryGetValue<string>(nameof(ImplementAttribute.Name), out implementationName) ||
                    string.IsNullOrWhiteSpace(implementationName))
                {
                    implementationName = typeSymbol.Name[1..];
                }

                string? keywords = null;
                args.TryGetValue(nameof(ImplementAttribute.Keywords), out keywords);



                // Create our Generate Info
                GenerateInfo generateInfo = new(typeSymbol)
                {
                    ImplementationTypeName = implementationName!,
                };



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
                foreach (ISymbol member in members)
                {                   
                    MemberSig memberSig = MemberSig.FromSymbol(member);

                    generateInfo.Members.Add(memberSig);
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
        // Check the interfaces
        List<IInterfaceImplementationWriter> implWriters = new()
        {
            // Always start with Property implementer
            new PropertyWriter(),
        };

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

        // Always end with default ToString()
        implWriters.Add(new ToStringImplWriter());

        using var codeBuilder = new CodeBuilder()
            .AutoGeneratedHeader()
            .Nullable(true)
            // Usings?
            .NewLine()
            .Namespace(generateInfo.InterfaceTypeSymbol.GetFQNamespace())
            // Implementation declaration
            .Enumerate(generateInfo.Visibility.GetFlags(), (cb, flag) => cb.Append(flag.ToString().ToLower()).Append(' '))
            .Enumerate(generateInfo.MemberKeywords.GetFlags(), (cb, flag) => cb.Append(flag.ToString().ToLower()).Append(' '))
            .Append(generateInfo.ObjType.ToString().ToLower()).Append(' ').Append(generateInfo.ImplementationTypeName)
            // Interfaces
            .AppendLine(" : ")
            .IndentBlock(ib =>
            {
                // The main interface, always
                ib.Value(generateInfo.InterfaceTypeSymbol);
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
            })
            .TrimEnd();

        string filename = $"{generateInfo.InterfaceTypeSymbol.GetFQN()}.g.cs";
        string code = codeBuilder.ToString();

        Debugger.Break();

        SourceCode sourceCode = new SourceCode(filename, code);
        yield return sourceCode;
    }

}

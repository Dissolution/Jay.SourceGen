using Jay.SourceGen.Text;

using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Text;

using System.Diagnostics;
using System.Text;

namespace Jay.SourceGen;

public readonly record struct SourceCode(string FileName, SourceText Code)
{
    public SourceCode(string hintName, string code)
        : this(hintName, SourceText.From(code, Encoding.UTF8))
    {

    }
}

public abstract class BaseIncrementalGenerator
{

}

public abstract class AttributeTypeDeclarationGenerator : BaseIncrementalGenerator, IIncrementalGenerator
{
    public abstract string AttributeFQN { get; }

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        // Add any post-init output files
        context.RegisterPostInitializationOutput(ctx =>
        {
            foreach (var sourceCode in GetPostInitOutput())
            {
                ctx.AddSource(sourceCode.FileName, sourceCode.Code);
            }
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

    protected void Process(Compilation compilation,
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

    protected virtual IEnumerable<SourceCode> GetPostInitOutput()
    {
        yield break;
    }

    protected abstract IEnumerable<SourceCode> ProcessType(
        TypeDeclarationSyntax typeDeclarationSyntax,
        INamedTypeSymbol typeSymbol);


}

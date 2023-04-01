namespace Jay.SourceGen.Extensions;

public static class SyntaxNodeExtensions
{
    public static ISymbol? GetDeclaredSymbol(this SyntaxNode node, Compilation compilation)
    {
        var model = compilation.GetSemanticModel(node.SyntaxTree);
        return model.GetDeclaredSymbol(node);
    }
    
    public static string? GetNamespace(this SyntaxNode syntaxNode)
    {
        // determine the namespace the class is declared in, if any
        string? nameSpace = null;
        SyntaxNode? potentialNamespaceParent = syntaxNode.Parent;
        while (potentialNamespaceParent != null &&
               potentialNamespaceParent is not NamespaceDeclarationSyntax &&
               potentialNamespaceParent is not FileScopedNamespaceDeclarationSyntax)
        {
            potentialNamespaceParent = potentialNamespaceParent.Parent;
        }

        if (potentialNamespaceParent is BaseNamespaceDeclarationSyntax namespaceParent)
        {
            nameSpace = namespaceParent.Name.ToString();
            while (true)
            {
                if (namespaceParent.Parent is not NamespaceDeclarationSyntax parent)
                {
                    break;
                }

                namespaceParent = parent;
                nameSpace = $"{namespaceParent.Name}.{nameSpace}";
            }
        }

        return nameSpace;
    }
}

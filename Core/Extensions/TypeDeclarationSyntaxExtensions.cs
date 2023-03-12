using Microsoft.CodeAnalysis.CSharp;

namespace Jay.SourceGen.Extensions;

public static class TypeDeclarationSyntaxExtensions
{
    public static bool HasKeyword(
        this TypeDeclarationSyntax typeDeclarationSyntax,
        SyntaxKind keyword)
    {
        return typeDeclarationSyntax.Modifiers.Any(m => m.IsKind(keyword));
    }
}
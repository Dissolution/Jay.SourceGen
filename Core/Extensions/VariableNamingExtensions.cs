using Microsoft.CodeAnalysis.CSharp;

namespace Jay.SourceGen.Extensions;

public static class VariableNamingExtensions
{    
     private static readonly ImmutableHashSet<string> _keywords = SyntaxFacts
                .GetKeywordKinds()
                .Select(SyntaxFacts.GetText)
                .ToImmutableHashSet();
    
    public static string GetVariableName(this ITypeSymbol typeSymbol) => typeSymbol.Name.ToVariableName();

    public static string ToVariableName(this string? name) => ToVariableName(name.AsSpan());

    public static string ToVariableName(this ReadOnlySpan<char> name)
    {
        int nameLen = name.Length;
        if (nameLen == 0) return "_";

        // Convert to camel-case, plus space for possible leading '@'
        Span<char> buffer = stackalloc char[nameLen];
        buffer[0] = char.ToLower(name[0]);
        name.Slice(1).CopyTo(buffer.Slice(1));
        string varName = buffer.ToString();
        // Check if we have to escape the name
        if (!SyntaxFacts.IsValidIdentifier(varName) || _keywords.Contains(varName))
        {
            return $"@{varName}";
        }
        return varName;
    }
}

namespace Jay.SourceGen.EntityGen;

internal static class Code
{
    public const string EntityAttributeFQN = $"Jay.SourceGen.EntityGen.Attributes.{nameof(EntityAttribute)}";
    public const string KeyAttributeFQN = $"Jay.SourceGen.EntityGen.Attributes.{nameof(KeyAttribute)}";
    public const string DisplayAttributeFQN = $"Jay.SourceGen.EntityGen.Attributes.{nameof(DisplayAttribute)}";
    public const string DisposeAttributeFQN = $"Jay.SourceGen.EntityGen.Attributes.{nameof(DisposeAttribute)}";

    public static string[] MemberAttributeFQNs = new string[3] { KeyAttributeFQN, DisplayAttributeFQN, DisposeAttributeFQN };
}
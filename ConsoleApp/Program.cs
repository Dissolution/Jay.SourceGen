using Jay.SourceGen.EnumGen.Attributes;
using System.Diagnostics;


TestEnum none = TestEnum.Alpha;

string? str = none.ToString();

Debugger.Break();

Console.WriteLine("Press [Enter] to close.");
Console.ReadLine();


[Enum]
public readonly struct TestEnum
{
    public static readonly TestEnum None = new();
    public static readonly TestEnum Alpha = new();
    public static readonly TestEnum Beta = new();
}
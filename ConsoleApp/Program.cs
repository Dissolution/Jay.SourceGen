using Jay.SourceGen.EnumGen.Attributes;
using Microsoft.CodeAnalysis.CSharp;
using System.Diagnostics;


TestEnum testEnum = default;


Debugger.Break();

Console.WriteLine("Press [Enter] to close.");
Console.ReadLine();


[Enum]
public readonly struct TestEnum
{
    public static readonly TestEnum None = default;
}
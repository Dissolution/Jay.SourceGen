using Jay.SourceGen.EnumGen.Attributes;
using System.Diagnostics;
using ConsoleApp;


TestEnum none = TestEnum.Alpha;

string? str = none.ToString();

bool isDefault = none == default;
var memberCount = TestEnum.Members.Count;

Debugger.Break();

Console.WriteLine("Press [Enter] to close.");
Console.ReadLine();



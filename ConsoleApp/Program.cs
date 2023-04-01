using Jay.SourceGen.ConsoleApp;

var entity = new Entity(4);

string? str = entity.ToString();

bool eqN = entity.Equals(new Entity(5));
bool eqY = entity.Equals(new Entity(4));


Console.WriteLine("Press [Enter] to close.");
Console.ReadLine();








//TestEnum none = TestEnum.Alpha;

//string? str = none.ToString();

//bool isDefault = none == default;
//var memberCount = TestEnum.Members.Count;

//Debugger.Break();
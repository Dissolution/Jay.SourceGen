using System.Dynamic;

namespace Jay.SourceGen.InterfaceGen.Scratch;

public class Test
{
    public Test()
    {
        InterfaceInfo interfaceInfo = new();
        //SourceCode sourceCode = InterfaceImplSourceCodeGen.GenerateSourceCode(interfaceInfo);



    }
}

[Implement(Keywords = "public readonly struct", Name = "TestStruct")]
public interface ITest
{

}

public static class InterfaceImplSourceCodeGen
{
    // setup any sort of static/cached stuff

    //internal static 

    //public static SourceCode GenerateSourceCode(dynamic interfaceInfo)
    //{

    //}
}



public interface IInterfaceImplementer
{
    bool CanImplement(TypeSig interfaceSig);

    IIPropertyGenerator? PropertyGenerator { get;}
}


public class InterfaceInfo : DynamicObject
{
    public IReadOnlyList<TypeSig> Interfaces { get; }
}

public partial class IIPropertyGenerator
{

}

internal partial class IICodeGenerator
{
    protected InterfaceInfo _interfaceInfo;



    public IICodeGenerator(InterfaceInfo interfaceInfo)
    {
        _interfaceInfo = interfaceInfo;
    }




    public SourceCode GenerateSourceCode()
    {
        // Assume our default Property generator
        dynamic propGen = null!;

        // What interfaces do we have to implement?
        var interfaces = _interfaceInfo.Interfaces;

        foreach (var iface in interfaces)
        {

        }



        using var codeBuilder = new CodeBuilder();


        throw new NotImplementedException();
    }
}
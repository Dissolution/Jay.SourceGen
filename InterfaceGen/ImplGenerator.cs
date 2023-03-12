//using Jay.SourceGen.InterfaceGen.Attributes;

//using Microsoft.CodeAnalysis;

//using System;
//using System.Collections.Generic;
//using System.Text;

//namespace Jay.SourceGen.InterfaceGen;

//[Generator]
//public sealed class ImplGenerator : AttributeTypeDeclarationGenerator,
//    IIncrementalGenerator
//{
//    public override string AttributeFQN => $"Jay.SourceGen.InterfaceGen.Attributes.{nameof(EntityAttribute)}";

//    protected override IEnumerable<SourceCode> ProcessType(TypeSymbolInfo typeSymbolInfo)
//    {
//        InterfaceCodeGen icg = new();
//        var sourceCode = icg.Generate(typeSymbolInfo);
//        yield return sourceCode;
//    }
//}

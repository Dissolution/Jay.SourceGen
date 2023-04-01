using System;
using System.Collections.Generic;
using System.Text;

namespace Jay.SourceGen.Extensions
{
    public static class ParameterSymbolExtensions
    {
        public static bool IsType<T>(this IParameterSymbol parameterSymbol)
        {
            return parameterSymbol.Type.GetFQN() == typeof(T).FullName;
        }
    }
}

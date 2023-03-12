namespace Jay.SourceGen.Extensions;

public static class TypedConstantExtensions
{
     public static object? GetObjectValue(this TypedConstant typedConstant)
    {
        if (typedConstant.Kind == TypedConstantKind.Array)
        {
            var values = typedConstant.Values;
            object?[] array = new object?[values.Length];
            for (var i = 0; i < values.Length; i++)
            {
                array[i] = GetObjectValue(values[i]);
            }
            return array;
        }
        else
        {
            return typedConstant.Value;
        }
    }
}

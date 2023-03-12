namespace Jay.SourceGen.Text;

public static class EnumerableExtensions
{
    public static CodeBuilder Enumerate<T>(
        this CodeBuilder codeBuilder,
        IEnumerable<T>? values,
        CBIA<T>? perValueAction)
    {
        if (values is null || perValueAction is null) return codeBuilder;
        using var e = values.GetEnumerator();
        int index = 0;
        if (!e.MoveNext()) return codeBuilder;
        perValueAction?.Invoke(codeBuilder, e.Current, index);
        while (e.MoveNext())
        {
            index++;
            perValueAction?.Invoke(codeBuilder, e.Current, index);
        }
        return codeBuilder;
    }

    public static CodeBuilder Enumerate<T>(
        this CodeBuilder codeBuilder,
        IEnumerable<T>? values,
        CBA<T>? perValueAction)
    {
        if (values is null || perValueAction is null) return codeBuilder;
        using var e = values.GetEnumerator();
        if (!e.MoveNext()) return codeBuilder;
        perValueAction?.Invoke(codeBuilder, e.Current);
        while (e.MoveNext())
        {
            perValueAction?.Invoke(codeBuilder, e.Current);
        }
        return codeBuilder;
    }

    public static CodeBuilder Delimit<T>(
        this CodeBuilder codeBuilder,
        CBA? delimitAction,
        IEnumerable<T>? values,
        CBIA<T>? perValueAction)
    {
        if (values is null || (delimitAction is null && perValueAction is null)) return codeBuilder;
        using var e = values.GetEnumerator();
        int index = 0;
        if (!e.MoveNext()) return codeBuilder;
        perValueAction?.Invoke(codeBuilder, e.Current, index);
        while (e.MoveNext())
        {
            delimitAction?.Invoke(codeBuilder);
            index++;
            perValueAction?.Invoke(codeBuilder, e.Current, index);
        }
        return codeBuilder;
    }

    public static CodeBuilder Delimit<T>(
        this CodeBuilder codeBuilder,
        CBA? delimitAction,
        IEnumerable<T>? values,
        CBA<T>? perValueAction)
    {
        if (values is null || (delimitAction is null && perValueAction is null)) return codeBuilder;
        using var e = values.GetEnumerator();
        if (!e.MoveNext()) return codeBuilder;
        perValueAction?.Invoke(codeBuilder, e.Current);
        while (e.MoveNext())
        {
            delimitAction?.Invoke(codeBuilder);
            perValueAction?.Invoke(codeBuilder, e.Current);
        }
        return codeBuilder;
    }

     public static CodeBuilder Delimit<T>(
        this CodeBuilder codeBuilder,
        string delimiter,
        IEnumerable<T>? values,
        CBA<T>? perValueAction)
    {
        if (string.IsNullOrEmpty(delimiter))
            return Enumerate<T>(codeBuilder, values, perValueAction);
        return Delimit<T>(codeBuilder, b => b.Format(delimiter), values, perValueAction);
    }


}
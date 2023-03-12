namespace Jay.SourceGen.Text;

public static class RemoveExtensions
{
    public static bool TryRemove(this CodeBuilder textBuilder, Range range)
    {
        (int offset, int length) = range.GetOffsetAndLength(textBuilder.Length);
        if ((uint)offset + (uint)length > (uint)textBuilder.Length) return false;
        if (length > 0)
        {
            var written = textBuilder.Written;
            TextHelper.CopyTo(written.Slice(offset + length), written.Slice(offset));
            textBuilder.Length -= length;
        }
        return true;
    }

    public static bool TryRemove(this CodeBuilder textBuilder, Range range, [NotNullWhen(true)] out string? slice)
    {
        (int offset, int length) = range.GetOffsetAndLength(textBuilder.Length);
        if ((uint)offset + (uint)length > (uint)textBuilder.Length)
        {
            slice = null;
            return false;
        }
        if (length > 0)
        {
            var written = textBuilder.Written;
            slice = written.Slice(offset, length).ToString();
            TextHelper.CopyTo(written.Slice(offset + length), written.Slice(offset));
            textBuilder.Length -= length;
        }
        else
        {
            slice = "";
        }
        return true;
    }
    
    public static bool TryRemove(this CodeBuilder textBuilder, int index)
    {
        if ((uint)index >= (uint)textBuilder.Length) return false;
        var written = textBuilder.Written;
        TextHelper.CopyTo(written.Slice(index + 1), written.Slice(index));
        textBuilder.Length -= 1;
        return true;
    }
    
    public static bool TryRemove(this CodeBuilder textBuilder, int index, out char ch)
    {
        if ((uint)index >= (uint)textBuilder.Length)
        {
            ch = default;
            return false;
        }
        var written = textBuilder.Written;
        ch = written[index];
        TextHelper.CopyTo(written.Slice(index + 1), written.Slice(index));
        textBuilder.Length -= 1;
        return true;
    }
    
    public static bool TryRemove(this CodeBuilder textBuilder, int start, int length)
    {
        if ((uint)start + (uint)length > (uint)textBuilder.Length) return false;
        if (length > 0)
        {
            var written = textBuilder.Written;
            TextHelper.CopyTo(written.Slice(start + length), written.Slice(start));
            textBuilder.Length -= length;
        }
        return true;
    }

    public static bool TryRemove(this CodeBuilder textBuilder, int start, int length, [NotNullWhen(true)] out string? slice)
    {
        if ((uint)start + (uint)length > (uint)textBuilder.Length)
        {
            slice = null;
            return false;
        }
        if (length > 0)
        {
            var written = textBuilder.Written;
            slice = written.Slice(start, length).ToString();
            TextHelper.CopyTo(written.Slice(start + length), written.Slice(start));
            textBuilder.Length -= length;
        }
        else
        {
            slice = "";
        }
        return true;
    }
}
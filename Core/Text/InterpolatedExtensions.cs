namespace Jay.SourceGen.Text;

public static class InterpolatedExtensions
{
    #if NET6_0_OR_GREATER
    public static ref StackTextBuilder Interpolate(this ref StackTextBuilder textBuilder,
        [InterpolatedStringHandlerArgument(nameof(textBuilder))]
        ref StackTextBuilder interpolatedString)
    {
        // The writing has already happened by the time we get into this method!
        var tbStr = textBuilder.ToString();
        var isStr = interpolatedString.ToString();
        
        return ref interpolatedString;
    }
#endif
}
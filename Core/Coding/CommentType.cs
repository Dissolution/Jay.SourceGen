namespace Jay.SourceGen.Text;

public enum CommentType
{
    /// <summary>
    /// <c>// comment</c><br/>
    /// <i>or</i><br/>
    /// <c>// comment 1<br/>
    ///    // comment 2<br/>
    /// </c>
    /// </summary>a
    SingleLine,

    /// <summary>
    /// <c>/* comment */</c><br/>
    /// <i>or</i><br/>
    /// <c>
    /// /* comment 1<br/>
    ///  * comment 2<br/>
    ///  */<br/>
    /// </c>
    /// </summary>
    MultiLine,

    /// <summary>
    /// <c>/// comment</c><br />
    /// <i>or</i><br />
    /// <c>
    /// /// comment 1<br />
    /// /// comment 2<br />
    /// </c>
    /// </summary>
    XML,
}
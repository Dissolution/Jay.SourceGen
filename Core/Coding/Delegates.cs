using Jay.SourceGen.Coding;

namespace Jay.SourceGen.Text;

/// <summary>
/// <b>C</b>ode <b>B</b>uilder <b>A</b>ction <br/>
/// <see cref="System.Action{T}">Action&lt;CodeBuilder&gt;</see>
/// </summary>
/// <param name="codeBuilder">The <see cref="CodeBuilder"/> to build upon</param>
public delegate void CBA(CodeBuilder codeBuilder);

/// <summary>
/// <b>C</b>ode <b>B</b>uilder <b>A</b>ction with <typeparamref name="T"/> value <br/>
/// <see cref="System.Action{T, T}">Action&lt;CodeBuilder, T&gt;</see>
/// </summary>
/// <typeparam name="T">The <see cref="Type"/> of <paramref name="value"/> to build with</typeparam>
/// <param name="codeBuilder">The <see cref="CodeBuilder"/> to build upon</param>
/// <param name="value">The <typeparamref name="T"/> value to build with</param>
public delegate void CBA<in T>(CodeBuilder codeBuilder, T value);

/// <summary>
/// <b>C</b>ode <b>B</b>uilder <b>T</b>ext <b>A</b>ction <br/>
/// <see cref="System.Action{T, T}">Action&lt;CodeBuilder, ReadOnlySpan&lt;char&gt;&gt;</see>
/// </summary>
/// <typeparam name="T">The <see cref="Type"/> of <paramref name="value"/> to build with</typeparam>
/// <param name="codeBuilder">The <see cref="CodeBuilder"/> to build upon</param>
/// <param name="text">The <see cref="System.ReadOnlySpan{T}">ReadOnlySpan&lt;char&gt;</see> value to build with</param>
public delegate void CBTA(CodeBuilder codeBuilder, ReadOnlySpan<char> text);

/// <summary>
/// <b>C</b>ode <b>B</b>uilder <b>I</b>ndexed <b>A</b>ction with <typeparamref name="T"/> value <br/>
/// <see cref="System.Action{T, T, T}">Action&lt;CodeBuilder, T, int&gt;</see>
/// </summary>
/// <typeparam name="T">The <see cref="Type"/> of <paramref name="value"/> to build with</typeparam>
/// <param name="codeBuilder">The <see cref="CodeBuilder"/> to build upon</param>
/// <param name="value">The <typeparamref name="T"/> value to build with</param>
/// <param name="index">The current index of the <paramref name="value"/> being enumerated</param>
public delegate void CBIA<in T>(CodeBuilder codeBuilder, T value, int index);

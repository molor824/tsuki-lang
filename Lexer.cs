using System.Text;
using System.Globalization;

public class Lexer
{
    public static readonly HashSet<char> Symbols = new()
    {
        // arithmetic
        '+', '-', '/', '*',
        // comparison
        '<', '>', '=',
        // logical
        '|', '&', '!',
        // brackets
        '[', ']', '{', '}', '(', ')',
        // other
        '.', ',', ';', ':', '?'
    };
    public string Source;
    private int _index = -1;

    private enum NumType
    {
        Int,
        Float,
        Hex,
        Binary,
        Exponent
    }

    public Lexer(string path)
    {
        var source = File.ReadAllText(path);
        var builder = new StringBuilder();

        for (var i = 0; i < source.Length; i++)
        {
            var ch = source[i];

            if (ch == '\r')
            {
                if (source.TryGet(i + 1, out ch) && ch == '\n')
                    continue;

                ch = '\n';
            }

            builder.Append(ch);
        }

        if (Program.Debug)
        {
            Console.WriteLine($"Raw source: {source.Length * 2}\nUpdated source: {builder.Length * 2}");
        }

        Source = builder.ToString();
    }

    private static bool ConvertEscape(ReadOnlySpan<char> source, out string result, out Error err)
    {
        result = null!;
        err = null!;

        var builder = new StringBuilder();

        for (var i = 0; i < source.Length; i++)
        {
            var ch = source[i];

            if (ch == '\\')
            {
                i++;
                if (source.TryGet(i, out ch))
                {
                    switch (ch)
                    {
                        case 'a':
                            ch = '\a';
                            break;
                        case 'b':
                            ch = '\b';
                            break;
                        case 'n':
                            ch = '\n';
                            break;
                        case 't':
                            ch = '\t';
                            break;
                        case '\\':
                            ch = '\\';
                            break;
                        case '\'':
                            ch = '\'';
                            break;
                        case '"':
                            ch = '"';
                            break;
                        case '0':
                            ch = '\0';
                            break;
                        case 'x' or 'u':
                            var len = ch == 'x' ? 2 : 4;
                            var start = i + 1;
                            var complete = true;

                            for (i = start; i - start > len; i++)
                            {
                                if (source.TryGet(i, out ch))
                                {
                                    if (char.IsDigit(ch) || char.ToLower(ch) is >= 'a' and <= 'f') continue;

                                    complete = false;
                                    break;
                                }

                                complete = false;
                                break;
                            }

                            if (!complete)
                            {
                                err = new Error("Incomplete escape sequence", i - 1);
                                return false;
                            }

                            i--;

                            ch = (char)ushort.Parse(source.Slice(start, len), NumberStyles.HexNumber);
                            break;
                        default:
                            err = new Error("Unexpected escape character", i);
                            return false;
                    }
                }
                else
                {
                    err = new Error("Expected escape character", i - 1);
                    return false;
                }
            }

            builder.Append(ch);
        }

        result = builder.ToString();
        return true;
    }

    static bool ParseInt(ReadOnlySpan<char> source, ulong baseNum, out ulong result, out Error err)
    {
        if (baseNum > 16) throw new Exception("Max supported base is 16");

        result = default;
        err = default!;

        for (var i = 0; i < source.Length; i++)
        {
            var digit = (ulong)(source[i] - '0');

            if (digit > 9) digit = (ulong)(char.ToLower(source[i]) - 'a' + 10);
            // just ignore everything that isn't digit
            // it's already checked which region is digit before being called
            // no need to throw error
            if (digit >= 16) continue;

            if (result * baseNum + digit < result)
            {
                err = new Error("Integer overflow", 0);
                return false;
            }

            result = result * baseNum + digit;
        }

        return true;
    }

    static bool ParseFloat(ReadOnlySpan<char> source, out double result, out Error err)
    {
        result = default;
        err = default!;

        for (var i = 0; i < source.Length; i++)
        {
            var digit = (double)(source[i] - '0');

            if (source[i] == '.')
            {
                var frac = 0.0;

                for (var j = source.Length - 1; j > i; j--)
                {
                    digit = source[j] - '0';

                    frac = frac / 10 + digit;
                }

                result += frac / 10;
                return true;
            }

            if (result * 10 + digit < result)
            {
                err = new Error("Float overflow", 0);
                return false;
            }

            result = result * 10 + digit;
        }

        return true;
    }
    public bool TryCurrent(out char result)
    {
        return Source.TryGet(_index, out result);
    }

    public bool TryNext(out char result)
    {
        _index++;
        return TryCurrent(out result);
    }

    public bool TryPrevious(out char result)
    {
        _index--;
        return TryCurrent(out result);
    }

    public bool Lex(out Token? result, out Error err)
    {
        result = null!;
        err = null!;

        while (TryNext(out var ch))
        {
            if (char.IsWhiteSpace(ch)) continue;
            if (ch == '/')
            {
                if (TryNext(out ch))
                {
                    if (ch == '/')
                    {
                        while (TryNext(out ch) && ch != '\n')
                        {
                        }

                        continue;
                    }
                    if (ch == '*')
                    {
                        while (TryNext(out ch))
                            if (ch == '*' && TryNext(out ch) && ch == '/') break;

                        continue;
                    }
                }

                TryPrevious(out ch);
            }

            if (char.IsLetter(ch) || ch == '_')
            {
                var start = _index;

                while (TryNext(out ch) && char.IsLetterOrDigit(ch) || ch == '_')
                {
                }

                var end = _index;
                _index--;

                result = new Ident(Source.Substring(start, end - start), start);
                return true;
            }

            if (char.IsDigit(ch))
            {
                var start = _index;
                var type = NumType.Int;
                // false meaning -, true meaning +
                var expSign = true;

                if (ch == '0' && TryNext(out ch))
                {
                    start += 2;

                    if (ch == 'x') type = NumType.Hex;
                    else if (ch == 'b') type = NumType.Binary;
                    else
                    {
                        start -= 2;
                        _index--;
                    }
                }

                while (TryNext(out ch))
                {
                    ch = char.ToLower(ch);

                    if (ch == '.')
                    {
                        if (type != NumType.Int) break;

                        type = NumType.Float;
                        continue;
                    }
                    if (ch == 'e')
                    {
                        if (type != NumType.Int && type != NumType.Float) break;

                        if (TryNext(out ch))
                        {
                            if (ch == '+') expSign = true;
                            else if (ch == '-') expSign = false;
                            else _index--;
                        }

                        type = NumType.Exponent;
                        continue;
                    }
                    if (type == NumType.Hex && (char.IsDigit(ch) || ch is >= 'a' and <= 'f'))
                        continue;
                    if (type == NumType.Binary && ch is '0' or '1')
                        continue;
                    if (char.IsDigit(ch))
                        continue;

                    break;
                }

                var end = _index;

                _index--;

                var src = Source.AsSpan(start, end - start);

                switch (type)
                {
                    case NumType.Int or NumType.Binary or NumType.Hex:
                        var baseNum = 10ul;

                        if (type == NumType.Binary) baseNum = 2;
                        else if (type == NumType.Hex) baseNum = 16;

                        if (ParseInt(src, baseNum, out var rint, out err))
                        {
                            result = new IntLiteral(rint, start);
                            return true;
                        }
                        err.Index += start;
                        return false;
                    case NumType.Float:
                        if (ParseFloat(src, out var rfloat, out err))
                        {
                            result = new FloatLiteral(rfloat, start);
                            return true;
                        }

                        err.Index += start;
                        return false;
                    case NumType.Exponent:
                        var exponent = 0;

                        for (var i = 0; i < src.Length; i++)
                        {
                            if (src[i] is 'e' or 'E')
                            {
                                exponent = i;
                                break;
                            }
                        }

                        var floatStr = src.Slice(0, exponent);
                        var exponentStr = src.Slice(exponent + 1, src.Length - exponent - 1);

                        if (!ParseFloat(floatStr, out rfloat, out err))
                        {
                            err.Index += start;
                            return false;
                        }

                        if (!ParseInt(exponentStr, 10, out rint, out err))
                        {
                            err.Index += start;
                            return false;
                        }

                        result = new FloatLiteral(rfloat * Math.Pow(10, (double)rint * (expSign ? 1 : -1)), start);
                        return true;
                }
            }

            if (ch == '\'')
            {
                var start = _index + 1;
                var ended = false;

                while (TryNext(out ch))
                {
                    if (ch == '\\')
                    {
                        _index++;
                        continue;
                    }

                    if (ch == '\n') break;

                    if (ch == '\'')
                    {
                        ended = true;
                        break;
                    }
                }

                if (!ended)
                {
                    err = new Error("Unterminated character literal", _index - 1);
                    return false;
                }

                var end = _index;

                if (!ConvertEscape(Source.AsSpan(start, end - start), out var rstr, out err))
                {
                    err.Index += start;
                    return false;
                }

                if (rstr.Length > 1)
                {
                    err = new Error("Too much characters", start + rstr.Length - 1);
                    return false;
                }

                if (rstr.Length < 1)
                {
                    err = new Error("Empty character literal", start + rstr.Length - 1);
                    return false;
                }

                result = new CharLiteral(rstr[0], start);
                return true;
            }

            if (ch == '"')
            {
                var start = _index + 1;
                var ended = false;

                while (TryNext(out ch))
                {
                    if (ch == '\\')
                    {
                        _index++;
                        continue;
                    }

                    if (ch == '\n') break;

                    if (ch == '"')
                    {
                        ended = true;
                        break;
                    }
                }

                if (!ended)
                {
                    err = new Error("Unterminated string literal", _index - 1);
                    return false;
                }

                var end = _index;
                if (!ConvertEscape(Source.AsSpan(start, end - start), out var rstr, out err))
                {
                    err.Index += start;
                    return false;
                }

                result = new StringLiteral(rstr, start);
                return true;
            }

            if (Symbols.Contains(ch))
            {
                result = new Symbol(ch, _index);
                return true;
            }

            err = new Error("Unexpected symbol", _index);
            return false;
        }

        return true;
    }
}
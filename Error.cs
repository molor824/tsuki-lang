using System.Text;

public class Error : Exception
{
    public string Reason;
    public int Index;

    public string OutputError(string source, string path)
    {
        var (line, column) = GetLocation(source, Index);

        var builder = new StringBuilder();
        var len = line.ToString().Length;

        builder.Append($"Error at: [{Path.GetFullPath(path)}, line: {line}, column: {column}]\n");
        builder.Append($"{line} | {GetLine(source, line)}\n");
        builder.Append(new string(' ', len + 2 + column));
        builder.Append("^ ");
        builder.Append(Reason);

        return builder.ToString();
    }

    private static (int, int) GetLocation(string source, int index)
    {
        var line = 1;
        var column = 1;

        var lastCh = '\0';

        for (var i = 0; i < index; i++)
        {
            if (lastCh == '\n')
            {
                line++;
                column = 1;
            }

            column++;

            lastCh = source[i];
        }

        return (line, column);
    }

    private static ReadOnlySpan<char> GetLine(string source, int line)
    {
        var start = 0;

        for (var i = 0; i < source.Length; i++)
        {
            var ch = source[i];

            if (ch != '\n') continue;

            line--;

            if (line <= 0)
            {
                return source.AsSpan(start, i - start);
            }

            start = i + 1;
        }

        return null;
    }

    public Error(string reason, int index)
    {
        Reason = reason;
        Index = index;
    }
}
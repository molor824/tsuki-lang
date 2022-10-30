using System.Text;

static class Program
{
    public static bool Debug = false;

    private static void Main(string[] args)
    {
        Console.OutputEncoding = Encoding.UTF8;

        if (args.Length == 0)
        {
            Console.WriteLine("Expected path");
            return;
        }

        var path = default(string);

        for (var i = 0; i < args.Length; i++)
        {
            if (args[i] == "--debug")
            {
                Debug = true;

                Console.WriteLine("Debug mode activated. Will print progress of the entire interpretation. Expect to see huge performance drops :)");
                continue;
            }

            path = args[i];
        }

        if (path == null)
        {
            Console.WriteLine("Expected a path");
            return;
        }

        if (!File.Exists(path))
        {
            Console.WriteLine("Invalid path");
            return;
        }

        var lexer = new Lexer(path);
        var tokens = new List<Token>();
        var errors = new List<Error>();

        while (true)
        {
            if (lexer.Lex(out var token, out var err))
            {
                if (token == null) break;
                
                if (Debug) Console.WriteLine($"{token.GetType()}: {token}");
                
                tokens.Add(token);
            }
            else errors.Add(err);
        }

        foreach (var e in errors)
        {
            Console.WriteLine(e.OutputError(lexer.Source, path));
        }
    }
}
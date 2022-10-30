public abstract class Token
{
    public int Index;

    public Token(int index)
    {
        Index = index;
    }
}

public class Symbol : Token
{
    public char Value;

    public Symbol(char value, int index) : base(index)
    {
        Value = value;
    }
    public override string ToString()
    {
        return $"'{Value}'";
    }
}
public class StringLiteral : Token
{
    public string Value;


    public StringLiteral(string value, int index) : base(index)
    {
        Value = value;
    }

    public override string ToString()
    {
        return $"\"{Value}\"";
    }
}
public class CharLiteral : Token
{
    public char Value;

    // converts the string to escape code if it's one
    public CharLiteral(char value, int index) : base(index)
    {
        Value = value;
    }

    public override string ToString()
    {
        return $"\'{Value}\'";
    }
}
public class IntLiteral : Token
{
    public ulong Value;

    public IntLiteral(ulong value, int index) : base(index)
    {
        Value = value;
    }

    public override string ToString()
    {
        return $"{Value}";
    }
}

public class FloatLiteral : Token
{
    public double Value;

    public FloatLiteral(double value, int index) : base(index)
    {
        Value = value;
    }

    public override string ToString()
    {
        return $"{Value}";
    }
}
public class Ident : Token
{
    public string Value;

    public Ident(string value, int index) : base(index)
    {
        Value = value;
    }

    public override string ToString()
    {
        return $"{Value}";
    }
}
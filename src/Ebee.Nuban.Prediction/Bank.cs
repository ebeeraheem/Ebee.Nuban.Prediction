namespace Ebee.Nuban.Prediction;

public class Bank(string name, string code)
{
    public string Name { get; set; } = name;
    public string Code { get; set; } = code;

    public override string ToString() => $"{Name} ({Code})";

    public override bool Equals(object? obj) =>
        obj is Bank other &&
        string.Equals(Code, other.Code, StringComparison.OrdinalIgnoreCase);

    public override int GetHashCode() => Code.GetHashCode(StringComparison.OrdinalIgnoreCase);
}
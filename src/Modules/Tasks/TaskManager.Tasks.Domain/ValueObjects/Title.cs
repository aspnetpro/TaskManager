namespace TaskManager.Tasks.Domain.ValueObjects;

public sealed record Title
{
    public const int MaxLength = 200;

    private Title(string value) => Value = value;

    public string Value { get; }

    public static Result<Title> Create(string? value)
    {
        var trimmed = value?.Trim();

        if (string.IsNullOrEmpty(trimmed))
            return Error.Validation("Title.Empty", "Title is required.");

        if (trimmed.Length > MaxLength)
            return Error.Validation("Title.TooLong", $"Title must be {MaxLength} characters or less.");

        return new Title(trimmed);
    }

    public override string ToString() => Value;
}

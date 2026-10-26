namespace TaskManager.Tasks.Domain.ValueObjects;

public sealed record Description
{
    public const int MaxLength = 2000;

    private Description(string value) => Value = value;

    public string Value { get; }

    /// <summary>
    /// Description é opcional: null ou só espaços resultam em sucesso com valor null.
    /// </summary>
    public static Result<Description?> Create(string? value)
    {
        var trimmed = value?.Trim();

        if (string.IsNullOrEmpty(trimmed))
            return Result<Description?>.Success(null);

        if (trimmed.Length > MaxLength)
            return Error.Validation("Description.TooLong", $"Description must be {MaxLength} characters or less.");

        return new Description(trimmed);
    }

    public override string ToString() => Value;
}

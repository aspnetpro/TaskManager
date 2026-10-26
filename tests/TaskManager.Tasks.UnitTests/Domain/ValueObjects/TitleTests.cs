namespace TaskManager.Tasks.UnitTests.Domain.ValueObjects;

public sealed class TitleTests
{
    [Fact]
    public void Create_ValidValue_ReturnsTrimmedTitle()
    {
        var result = Title.Create("  Implement auth  ");

        result.IsSuccess.Should().BeTrue();
        result.Value.Value.Should().Be("Implement auth");
    }

    [Fact]
    public void Create_ExactlyMaxLength_Succeeds()
    {
        var result = Title.Create(new string('a', Title.MaxLength));

        result.IsSuccess.Should().BeTrue();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_EmptyValue_ReturnsEmptyError(string? value)
    {
        var result = Title.Create(value);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Title.Empty");
    }

    [Fact]
    public void Create_OverMaxLength_ReturnsTooLongError()
    {
        var result = Title.Create(new string('a', Title.MaxLength + 1));

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Title.TooLong");
    }
}

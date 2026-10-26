namespace TaskManager.Tasks.UnitTests.Domain.ValueObjects;

public sealed class DescriptionTests
{
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_EmptyValue_ReturnsSuccessWithNull(string? value)
    {
        var result = Description.Create(value);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeNull();
    }

    [Fact]
    public void Create_ValidValue_ReturnsTrimmedDescription()
    {
        var result = Description.Create(" details ");

        result.Value!.Value.Should().Be("details");
    }

    [Fact]
    public void Create_OverMaxLength_ReturnsTooLongError()
    {
        var result = Description.Create(new string('a', Description.MaxLength + 1));

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Description.TooLong");
    }
}

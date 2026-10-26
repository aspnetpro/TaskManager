using TaskManager.Auth.Contracts;
using static TaskManager.Notifications.UnitTests.EventHandlers.NotificationHandlerFixture;

namespace TaskManager.Notifications.UnitTests.EventHandlers;

public sealed class TaskAssignedEmailHandlerTests
{
    private readonly IUserDirectory _users = Substitute.For<IUserDirectory>();
    private readonly IEmailSender _email = Substitute.For<IEmailSender>();

    public TaskAssignedEmailHandlerTests()
    {
        _users.GetContactAsync(Assignee, Arg.Any<CancellationToken>()).Returns(new UserContact(Assignee, "dev@example.com", "Dev"));
        _email.SendAsync(Arg.Any<EmailMessage>(), Arg.Any<CancellationToken>()).Returns(Result.Success());
    }

    private TaskAssignedEmailHandler Handler() => new(_users, _email);

    [Fact]
    public async Task HandleAsync_NewAssignee_SendsEmailWithTaskTitle()
    {
        await Handler().HandleAsync(Assigned(Assignee, Lead), CancellationToken.None);

        await _email.Received(1).SendAsync(
            Arg.Is<EmailMessage>(m => m.To == "dev@example.com" && m.Subject.Contains("Review PR")),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_UserWithoutAccount_SkipsEmail()
    {
        await Handler().HandleAsync(Assigned(Guid.NewGuid(), Lead), CancellationToken.None);

        await _email.DidNotReceiveWithAnyArgs().SendAsync(default!, default);
    }

    [Fact]
    public async Task HandleAsync_ProviderFails_ThrowsSoOutboxRetries()
    {
        _email.SendAsync(Arg.Any<EmailMessage>(), Arg.Any<CancellationToken>())
            .Returns(Result.Failure(Error.Unexpected("Email.DeliveryFailed", "boom")));

        var act = () => Handler().HandleAsync(Assigned(Assignee, Lead), CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>();
    }
}

using TaskManager.Auth.Contracts;

namespace TaskManager.Notifications.Application.EventHandlers;

/// <summary>
/// Email para o novo responsável. Se o envio falhar, a exceção faz o Outbox
/// tentar de novo — e o usuário pode receber o email duas vezes. Duplicata de
/// email é aceitável; email perdido não é.
/// </summary>
public sealed class TaskAssignedEmailHandler(IUserDirectory users, IEmailSender emailSender)
    : IIntegrationEventHandler<TaskAssignedIntegrationEvent>
{
    public async Task HandleAsync(TaskAssignedIntegrationEvent integrationEvent, CancellationToken cancellationToken)
    {
        if (integrationEvent.AssigneeId is not { } assigneeId || assigneeId == integrationEvent.AssignedBy)
            return;

        var contact = await users.GetContactAsync(assigneeId, cancellationToken);
        if (contact is null)
            return;

        var sent = await emailSender.SendAsync(new EmailMessage(
            contact.Email,
            $"You were assigned: {integrationEvent.Title}",
            $"Hi {contact.FullName}, the task \"{integrationEvent.Title}\" is now yours."), cancellationToken);

        if (sent.IsFailure)
            throw new InvalidOperationException($"Email delivery failed: {sent.Error.Code}");
    }
}

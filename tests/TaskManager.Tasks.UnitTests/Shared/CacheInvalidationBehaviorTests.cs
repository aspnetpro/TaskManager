using MediatR;
using TaskManager.Shared.Application.Behaviors;
using TaskManager.Shared.Application.Caching;
using TaskManager.Tasks.Application.Features.Tasks;
using TaskManager.Tasks.Application.Features.Tasks.Delete;

namespace TaskManager.Tasks.UnitTests.Shared;

public sealed class CacheInvalidationBehaviorTests
{
    private readonly IWorkspaceCache _cache = Substitute.For<IWorkspaceCache>();

    [Fact]
    public async Task Handle_AfterHandler_RemovesCommandKeys()
    {
        var command = new DeleteTaskCommand(Guid.NewGuid());
        var sut = new CacheInvalidationBehavior<DeleteTaskCommand, Result>(_cache);

        await sut.Handle(command, _ => Task.FromResult(Result.Success()), CancellationToken.None);

        await _cache.Received(1).RemoveAsync(TaskCacheKeys.Details(command.TaskId), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_HandlerFails_StillInvalidates()
    {
        var command = new DeleteTaskCommand(Guid.NewGuid());
        var sut = new CacheInvalidationBehavior<DeleteTaskCommand, Result>(_cache);

        var result = await sut.Handle(command, _ => Task.FromResult<Result>(CommonErrors.ConcurrencyConflict), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        await _cache.Received(1).RemoveAsync(TaskCacheKeys.Details(command.TaskId), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_InvalidatesOnlyAfterHandlerRuns()
    {
        var command = new DeleteTaskCommand(Guid.NewGuid());
        var sut = new CacheInvalidationBehavior<DeleteTaskCommand, Result>(_cache);
        RequestHandlerDelegate<Result> next = _ =>
        {
            _cache.ReceivedCalls().Should().BeEmpty();
            return Task.FromResult(Result.Success());
        };

        await sut.Handle(command, next, CancellationToken.None);

        _cache.ReceivedCalls().Should().ContainSingle();
    }
}

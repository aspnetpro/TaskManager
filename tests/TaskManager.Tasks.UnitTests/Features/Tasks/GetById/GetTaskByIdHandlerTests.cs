using TaskManager.Shared.Application.Caching;
using TaskManager.Tasks.Application.Features.Tasks;
using TaskManager.Tasks.Application.Features.Tasks.GetById;

namespace TaskManager.Tasks.UnitTests.Features.Tasks.GetById;

public sealed class GetTaskByIdHandlerTests
{
    private readonly ITaskRepository _repository = Substitute.For<ITaskRepository>();
    private readonly IWorkspaceCache _cache = Substitute.For<IWorkspaceCache>();

    public GetTaskByIdHandlerTests()
    {
        // Cache sempre em miss: executa a factory recebida.
        _cache.GetOrCreateAsync(
                Arg.Any<string>(),
                Arg.Any<Func<CancellationToken, Task<Result<TaskDetailsResponse>>>>(),
                Arg.Any<CancellationToken>())
            .Returns(call => call.ArgAt<Func<CancellationToken, Task<Result<TaskDetailsResponse>>>>(1)(CancellationToken.None));
    }

    private GetTaskByIdHandler Handler() => new(_repository, _cache, TestUsers.Clock());

    [Fact]
    public async Task Handle_ExistingTask_UsesDetailsCacheKey()
    {
        var task = new TaskItemBuilder().Build();
        _repository.GetForReadAsync(task.Id, Arg.Any<CancellationToken>()).Returns(task);

        var result = await Handler().Handle(new GetTaskByIdQuery(task.Id), CancellationToken.None);

        result.Value.Id.Should().Be(task.Id);
        await _cache.Received(1).GetOrCreateAsync(
            TaskCacheKeys.Details(task.Id),
            Arg.Any<Func<CancellationToken, Task<Result<TaskDetailsResponse>>>>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_MissingTask_ReturnsNotFound()
    {
        var result = await Handler().Handle(new GetTaskByIdQuery(Guid.NewGuid()), CancellationToken.None);

        result.Error.Type.Should().Be(ErrorType.NotFound);
    }
}

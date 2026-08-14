namespace Widgets.UnitTests;

public sealed class RenameWidgetHandlerTests
{
    private static readonly DateTimeOffset Created = new(2026, 1, 2, 3, 4, 5, TimeSpan.Zero);
    private static readonly DateTimeOffset Renamed = Created.AddHours(1);

    [Fact]
    public async Task Handle_renames_the_widget_and_stamps_the_update_time()
    {
        // Arrange
        var id = Guid.NewGuid();
        var widget = Widget.Create(new WidgetId(id), "Old", 3, Created);
        widget.ClearDomainEvents();
        var repository = Substitute.For<IWidgetRepository>();
        repository.GetByIdAsync(Arg.Any<WidgetId>(), Arg.Any<CancellationToken>()).Returns(widget);
        var unitOfWork = new FakeUnitOfWork();
        var handler = new RenameWidgetHandler(repository, unitOfWork, new FakeClock(Renamed));

        // Act
        var result = await handler.Handle(new RenameWidgetCommand(id, "New"), CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        widget.Name.Should().Be("New");
        widget.UpdatedOnUtc.Should().Be(Renamed);
        widget.DomainEvents.Should().ContainSingle().Which.Should().BeOfType<WidgetRenamed>();
        repository.Received(1).Update(widget);
        unitOfWork.SaveChangesCallCount.Should().Be(1);
    }

    [Fact]
    public async Task Handle_returns_not_found_when_the_widget_is_missing()
    {
        // Arrange
        var repository = Substitute.For<IWidgetRepository>();
        repository.GetByIdAsync(Arg.Any<WidgetId>(), Arg.Any<CancellationToken>()).Returns((Widget?)null);
        var unitOfWork = new FakeUnitOfWork();
        var handler = new RenameWidgetHandler(repository, unitOfWork, new FakeClock(Renamed));

        // Act
        var result = await handler.Handle(new RenameWidgetCommand(Guid.NewGuid(), "New"), CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Type.Should().Be(ErrorType.NotFound);
        result.Error.Code.Should().Be("widget.not_found");
        repository.DidNotReceive().Update(Arg.Any<Widget>());
        unitOfWork.SaveChangesCallCount.Should().Be(0);
    }
}

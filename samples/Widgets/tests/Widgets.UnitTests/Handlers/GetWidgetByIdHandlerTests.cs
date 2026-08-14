namespace Widgets.UnitTests;

public sealed class GetWidgetByIdHandlerTests
{
    private static readonly DateTimeOffset Instant = new(2026, 1, 2, 3, 4, 5, TimeSpan.Zero);

    [Fact]
    public async Task Handle_projects_the_widget_when_found()
    {
        // Arrange
        var id = Guid.NewGuid();
        var widget = Widget.Create(new WidgetId(id), "Gadget", 9, Instant);
        var repository = Substitute.For<IWidgetRepository>();
        repository.FirstOrDefaultAsync(Arg.Any<ISpecification<Widget>>(), Arg.Any<CancellationToken>()).Returns(widget);
        var handler = new GetWidgetByIdHandler(repository);

        // Act
        var result = await handler.Handle(new GetWidgetByIdQuery(id), CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(new WidgetDto(id, "Gadget", 9));
    }

    [Fact]
    public async Task Handle_returns_not_found_when_absent()
    {
        // Arrange
        var repository = Substitute.For<IWidgetRepository>();
        repository.FirstOrDefaultAsync(Arg.Any<ISpecification<Widget>>(), Arg.Any<CancellationToken>())
            .Returns((Widget?)null);
        var handler = new GetWidgetByIdHandler(repository);

        // Act
        var result = await handler.Handle(new GetWidgetByIdQuery(Guid.NewGuid()), CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Type.Should().Be(ErrorType.NotFound);
        result.Error.Code.Should().Be("widget.not_found");
    }
}

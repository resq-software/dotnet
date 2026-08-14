namespace Widgets.UnitTests;

public sealed class ListWidgetsHandlerTests
{
    private static readonly DateTimeOffset Instant = new(2026, 1, 2, 3, 4, 5, TimeSpan.Zero);

    [Fact]
    public async Task Handle_returns_an_offset_page_of_projected_widgets()
    {
        // Arrange
        IReadOnlyList<Widget> widgets =
        [
            Widget.Create(WidgetId.New(), "A", 1, Instant),
            Widget.Create(WidgetId.New(), "B", 2, Instant),
        ];
        var repository = Substitute.For<IWidgetRepository>();
        repository.ListAsync(Arg.Any<ISpecification<Widget>>(), Arg.Any<CancellationToken>()).Returns(widgets);
        repository.CountAsync(Arg.Any<ISpecification<Widget>>(), Arg.Any<CancellationToken>()).Returns(5);
        var handler = new ListWidgetsHandler(repository);

        // Act
        var result = await handler.Handle(new ListWidgetsQuery(2, 10), CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        var page = result.Value;
        page.Page.Should().Be(2);
        page.PageSize.Should().Be(10);
        page.TotalRows.Should().Be(5);
        page.Items.Select(item => item.Name).Should().Equal("A", "B");
    }
}

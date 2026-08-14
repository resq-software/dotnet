namespace ResQ.Service.UnitTests;

public sealed class ListSamplesHandlerTests
{
    private static readonly DateTimeOffset Instant = new(2026, 1, 2, 3, 4, 5, TimeSpan.Zero);

    [Fact]
    public async Task Handle_returns_an_offset_page_of_projected_samples()
    {
        // Arrange
        IReadOnlyList<Sample> samples =
        [
            Sample.Create(SampleId.New(), "A", 1, Instant),
            Sample.Create(SampleId.New(), "B", 2, Instant),
        ];
        var repository = Substitute.For<ISampleRepository>();
        repository.ListAsync(Arg.Any<ISpecification<Sample>>(), Arg.Any<CancellationToken>()).Returns(samples);
        repository.CountAsync(Arg.Any<ISpecification<Sample>>(), Arg.Any<CancellationToken>()).Returns(5);
        var handler = new ListSamplesHandler(repository);

        // Act
        var result = await handler.Handle(new ListSamplesQuery(2, 10), CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        var page = result.Value;
        page.Page.Should().Be(2);
        page.PageSize.Should().Be(10);
        page.TotalRows.Should().Be(5);
        page.Items.Select(item => item.Name).Should().Equal("A", "B");
    }
}

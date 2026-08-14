namespace ResQ.Service.UnitTests;

public sealed class GetSampleByIdHandlerTests
{
    private static readonly DateTimeOffset Instant = new(2026, 1, 2, 3, 4, 5, TimeSpan.Zero);

    [Fact]
    public async Task Handle_projects_the_sample_when_found()
    {
        // Arrange
        var id = Guid.NewGuid();
        var sample = Sample.Create(new SampleId(id), "Gadget", 9, Instant);
        var repository = Substitute.For<ISampleRepository>();
        repository.FirstOrDefaultAsync(Arg.Any<ISpecification<Sample>>(), Arg.Any<CancellationToken>()).Returns(sample);
        var handler = new GetSampleByIdHandler(repository);

        // Act
        var result = await handler.Handle(new GetSampleByIdQuery(id), CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(new SampleDto(id, "Gadget", 9));
    }

    [Fact]
    public async Task Handle_returns_not_found_when_absent()
    {
        // Arrange
        var repository = Substitute.For<ISampleRepository>();
        repository.FirstOrDefaultAsync(Arg.Any<ISpecification<Sample>>(), Arg.Any<CancellationToken>())
            .Returns((Sample?)null);
        var handler = new GetSampleByIdHandler(repository);

        // Act
        var result = await handler.Handle(new GetSampleByIdQuery(Guid.NewGuid()), CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Type.Should().Be(ErrorType.NotFound);
        result.Error.Code.Should().Be("sample.not_found");
    }
}

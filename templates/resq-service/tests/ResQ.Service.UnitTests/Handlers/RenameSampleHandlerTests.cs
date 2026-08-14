namespace ResQ.Service.UnitTests;

public sealed class RenameSampleHandlerTests
{
    private static readonly DateTimeOffset Created = new(2026, 1, 2, 3, 4, 5, TimeSpan.Zero);
    private static readonly DateTimeOffset Renamed = Created.AddHours(1);

    [Fact]
    public async Task Handle_renames_the_sample_and_stamps_the_update_time()
    {
        // Arrange
        var id = Guid.NewGuid();
        var sample = Sample.Create(new SampleId(id), "Old", 3, Created);
        sample.ClearDomainEvents();
        var repository = Substitute.For<ISampleRepository>();
        repository.GetByIdAsync(Arg.Any<SampleId>(), Arg.Any<CancellationToken>()).Returns(sample);
        var unitOfWork = new FakeUnitOfWork();
        var handler = new RenameSampleHandler(repository, unitOfWork, new FakeClock(Renamed));

        // Act
        var result = await handler.Handle(new RenameSampleCommand(id, "New"), CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        sample.Name.Should().Be("New");
        sample.UpdatedOnUtc.Should().Be(Renamed);
        sample.DomainEvents.Should().ContainSingle().Which.Should().BeOfType<SampleRenamed>();
        repository.Received(1).Update(sample);
        unitOfWork.SaveChangesCallCount.Should().Be(1);
    }

    [Fact]
    public async Task Handle_returns_not_found_when_the_sample_is_missing()
    {
        // Arrange
        var repository = Substitute.For<ISampleRepository>();
        repository.GetByIdAsync(Arg.Any<SampleId>(), Arg.Any<CancellationToken>()).Returns((Sample?)null);
        var unitOfWork = new FakeUnitOfWork();
        var handler = new RenameSampleHandler(repository, unitOfWork, new FakeClock(Renamed));

        // Act
        var result = await handler.Handle(new RenameSampleCommand(Guid.NewGuid(), "New"), CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Type.Should().Be(ErrorType.NotFound);
        result.Error.Code.Should().Be("sample.not_found");
        repository.DidNotReceive().Update(Arg.Any<Sample>());
        unitOfWork.SaveChangesCallCount.Should().Be(0);
    }
}

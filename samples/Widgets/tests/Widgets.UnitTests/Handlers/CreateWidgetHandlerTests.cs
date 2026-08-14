namespace Widgets.UnitTests;

public sealed class CreateWidgetHandlerTests
{
    private static readonly DateTimeOffset Instant = new(2026, 1, 2, 3, 4, 5, TimeSpan.Zero);

    [Fact]
    public async Task Handle_persists_the_widget_and_returns_its_id()
    {
        // Arrange
        var repository = Substitute.For<IWidgetRepository>();
        Widget? added = null;
        repository.When(r => r.AddAsync(Arg.Any<Widget>(), Arg.Any<CancellationToken>()))
            .Do(call => added = call.Arg<Widget>());
        var unitOfWork = new FakeUnitOfWork();
        var clock = new FakeClock(Instant);
        var handler = new CreateWidgetHandler(repository, unitOfWork, clock);

        // Act
        var result = await handler.Handle(new CreateWidgetCommand("Gadget", 5), CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        added.Should().NotBeNull();
        result.Value.Should().Be(added!.Id.Value);
        added.Name.Should().Be("Gadget");
        added.Quantity.Should().Be(5);
        await repository.Received(1).AddAsync(Arg.Any<Widget>(), Arg.Any<CancellationToken>());
        unitOfWork.SaveChangesCallCount.Should().Be(1);
    }

    [Fact]
    public async Task Handle_stamps_both_timestamps_from_the_clock()
    {
        // Arrange
        var repository = Substitute.For<IWidgetRepository>();
        Widget? added = null;
        repository.When(r => r.AddAsync(Arg.Any<Widget>(), Arg.Any<CancellationToken>()))
            .Do(call => added = call.Arg<Widget>());
        var handler = new CreateWidgetHandler(repository, new FakeUnitOfWork(), new FakeClock(Instant));

        // Act
        await handler.Handle(new CreateWidgetCommand("Gadget", 5), CancellationToken.None);

        // Assert
        added!.CreatedOnUtc.Should().Be(Instant);
        added.UpdatedOnUtc.Should().Be(Instant);
    }

    [Fact]
    public async Task Handle_raises_a_single_WidgetCreated_event_that_a_dispatcher_records()
    {
        // Arrange
        var repository = Substitute.For<IWidgetRepository>();
        Widget? added = null;
        repository.When(r => r.AddAsync(Arg.Any<Widget>(), Arg.Any<CancellationToken>()))
            .Do(call => added = call.Arg<Widget>());
        var handler = new CreateWidgetHandler(repository, new FakeUnitOfWork(), new FakeClock(Instant));
        var dispatcher = new RecordingDomainEventDispatcher();

        // Act
        await handler.Handle(new CreateWidgetCommand("Gadget", 5), CancellationToken.None);
        await dispatcher.DispatchAsync(added!.DomainEvents, CancellationToken.None);

        // Assert
        dispatcher.Dispatched.Should().ContainSingle()
            .Which.Should().BeOfType<WidgetCreated>()
            .Which.Should().Match<WidgetCreated>(created =>
                created.Name == "Gadget" && created.Quantity == 5 && created.OccurredOnUtc == Instant);
    }
}

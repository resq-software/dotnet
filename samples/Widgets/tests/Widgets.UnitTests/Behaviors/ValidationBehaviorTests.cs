using FluentValidation;

namespace Widgets.UnitTests;

public sealed class ValidationBehaviorTests
{
    [Fact]
    public async Task Handle_short_circuits_with_a_validation_failure_and_does_not_invoke_next()
    {
        // Arrange
        IEnumerable<IValidator<CreateWidgetCommand>> validators = [new CreateWidgetValidator()];
        var behavior = new ValidationBehavior<CreateWidgetCommand, Result<Guid>>(validators);
        var nextCalled = false;
        RequestHandlerDelegate<Result<Guid>> next = () =>
        {
            nextCalled = true;
            throw new InvalidOperationException("The handler must not run when validation fails.");
        };

        // Act — empty name and negative quantity both fail CreateWidgetValidator's rules.
        var result = await behavior.Handle(
            new CreateWidgetCommand(string.Empty, -1), next, CancellationToken.None);

        // Assert
        nextCalled.Should().BeFalse();
        result.IsFailure.Should().BeTrue();
        result.Error.Type.Should().Be(ErrorType.Validation);
        result.Error.Code.Should().Be("validation.failed");
        result.Error.Message.Should().Contain("Name").And.Contain("Quantity");
    }

    [Fact]
    public async Task Handle_invokes_next_and_returns_its_result_when_validation_passes()
    {
        // Arrange
        IEnumerable<IValidator<CreateWidgetCommand>> validators = [new CreateWidgetValidator()];
        var behavior = new ValidationBehavior<CreateWidgetCommand, Result<Guid>>(validators);
        var expected = Result.Success(Guid.NewGuid());

        // Act
        var result = await behavior.Handle(
            new CreateWidgetCommand("Gadget", 5), () => Task.FromResult(expected), CancellationToken.None);

        // Assert
        result.Should().BeSameAs(expected);
    }
}

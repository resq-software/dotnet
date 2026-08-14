namespace Widgets.UnitTests;

public sealed class RenameWidgetValidatorTests
{
    private readonly RenameWidgetValidator _validator = new();

    [Fact]
    public void Valid_command_passes()
    {
        // Act
        var result = _validator.Validate(new RenameWidgetCommand(Guid.NewGuid(), "New name"));

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Empty_id_fails_on_the_id_property()
    {
        // Act
        var result = _validator.Validate(new RenameWidgetCommand(Guid.Empty, "New name"));

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(failure => failure.PropertyName == nameof(RenameWidgetCommand.Id));
    }

    [Fact]
    public void Empty_name_fails_on_the_name_property()
    {
        // Act
        var result = _validator.Validate(new RenameWidgetCommand(Guid.NewGuid(), ""));

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(failure => failure.PropertyName == nameof(RenameWidgetCommand.Name));
    }
}

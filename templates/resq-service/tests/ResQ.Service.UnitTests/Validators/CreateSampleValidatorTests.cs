namespace ResQ.Service.UnitTests;

public sealed class CreateSampleValidatorTests
{
    private readonly CreateSampleValidator _validator = new();

    [Fact]
    public void Valid_command_passes()
    {
        // Arrange
        var command = new CreateSampleCommand("Gadget", 3);

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Empty_name_fails_on_the_name_property()
    {
        // Act
        var result = _validator.Validate(new CreateSampleCommand("", 3));

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(failure => failure.PropertyName == nameof(CreateSampleCommand.Name));
    }

    [Fact]
    public void Name_over_200_characters_fails_on_the_name_property()
    {
        // Act
        var result = _validator.Validate(new CreateSampleCommand(new string('x', 201), 3));

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(failure => failure.PropertyName == nameof(CreateSampleCommand.Name));
    }

    [Fact]
    public void Negative_quantity_fails_on_the_quantity_property()
    {
        // Act
        var result = _validator.Validate(new CreateSampleCommand("Gadget", -1));

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(failure => failure.PropertyName == nameof(CreateSampleCommand.Quantity));
    }
}

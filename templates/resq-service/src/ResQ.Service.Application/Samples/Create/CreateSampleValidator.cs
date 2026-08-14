using FluentValidation;

namespace ResQ.Service.Application;

/// <summary>Validates <see cref="CreateSampleCommand"/>.</summary>
public sealed class CreateSampleValidator : AbstractValidator<CreateSampleCommand>
{
    /// <summary>Configures the rules.</summary>
    public CreateSampleValidator()
    {
        RuleFor(command => command.Name).NotEmpty().MaximumLength(200);
        RuleFor(command => command.Quantity).GreaterThanOrEqualTo(0);
    }
}

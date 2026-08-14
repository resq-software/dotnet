using FluentValidation;

namespace Widgets.Application;

/// <summary>Validates <see cref="CreateWidgetCommand"/>.</summary>
public sealed class CreateWidgetValidator : AbstractValidator<CreateWidgetCommand>
{
    /// <summary>Configures the rules.</summary>
    public CreateWidgetValidator()
    {
        RuleFor(command => command.Name).NotEmpty().MaximumLength(200);
        RuleFor(command => command.Quantity).GreaterThanOrEqualTo(0);
    }
}

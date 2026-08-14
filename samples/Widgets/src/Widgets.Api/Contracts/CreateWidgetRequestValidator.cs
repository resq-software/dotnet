using FluentValidation;

namespace Widgets.Api;

/// <summary>Validates <see cref="CreateWidgetRequest"/> at the HTTP boundary.</summary>
public sealed class CreateWidgetRequestValidator : AbstractValidator<CreateWidgetRequest>
{
    /// <summary>Configures the rules.</summary>
    public CreateWidgetRequestValidator()
    {
        RuleFor(request => request.Name).NotEmpty().MaximumLength(200);
        RuleFor(request => request.Quantity).GreaterThanOrEqualTo(0);
    }
}

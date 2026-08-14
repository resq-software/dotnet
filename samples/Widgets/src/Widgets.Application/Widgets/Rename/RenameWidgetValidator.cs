using FluentValidation;

namespace Widgets.Application;

/// <summary>Validates <see cref="RenameWidgetCommand"/>.</summary>
public sealed class RenameWidgetValidator : AbstractValidator<RenameWidgetCommand>
{
    /// <summary>Configures the rules.</summary>
    public RenameWidgetValidator()
    {
        RuleFor(command => command.Id).NotEmpty();
        RuleFor(command => command.Name).NotEmpty().MaximumLength(200);
    }
}

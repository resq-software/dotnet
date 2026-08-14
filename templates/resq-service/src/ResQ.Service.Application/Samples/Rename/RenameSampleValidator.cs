using FluentValidation;

namespace ResQ.Service.Application;

/// <summary>Validates <see cref="RenameSampleCommand"/>.</summary>
public sealed class RenameSampleValidator : AbstractValidator<RenameSampleCommand>
{
    /// <summary>Configures the rules.</summary>
    public RenameSampleValidator()
    {
        RuleFor(command => command.Id).NotEmpty();
        RuleFor(command => command.Name).NotEmpty().MaximumLength(200);
    }
}

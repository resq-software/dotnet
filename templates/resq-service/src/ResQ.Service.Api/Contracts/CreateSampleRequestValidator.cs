using FluentValidation;

namespace ResQ.Service.Api;

/// <summary>Validates <see cref="CreateSampleRequest"/> at the HTTP boundary.</summary>
public sealed class CreateSampleRequestValidator : AbstractValidator<CreateSampleRequest>
{
    /// <summary>Configures the rules.</summary>
    public CreateSampleRequestValidator()
    {
        RuleFor(request => request.Name).NotEmpty().MaximumLength(200);
        RuleFor(request => request.Quantity).GreaterThanOrEqualTo(0);
    }
}

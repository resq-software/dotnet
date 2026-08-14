using ResQ.BuildingBlocks.Application;
using ResQ.BuildingBlocks.Domain;
using Widgets.Domain;

namespace Widgets.Application;

/// <summary>Handles <see cref="CreateWidgetCommand"/>, stamping timestamps from <see cref="IClock"/>.</summary>
public sealed class CreateWidgetHandler(IWidgetRepository repository, IUnitOfWork unitOfWork, IClock clock)
    : ICommandHandler<CreateWidgetCommand, Guid>
{
    /// <inheritdoc />
    public async Task<Result<Guid>> Handle(CreateWidgetCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        var widget = Widget.Create(WidgetId.New(), command.Name, command.Quantity, clock.UtcNow);
        await repository.AddAsync(widget, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(widget.Id.Value);
    }
}

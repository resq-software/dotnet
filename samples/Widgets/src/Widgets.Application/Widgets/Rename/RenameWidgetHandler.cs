using ResQ.BuildingBlocks.Application;
using ResQ.BuildingBlocks.Domain;
using Widgets.Domain;

namespace Widgets.Application;

/// <summary>Handles <see cref="RenameWidgetCommand"/>, stamping the timestamp from <see cref="IClock"/>.</summary>
public sealed class RenameWidgetHandler(IWidgetRepository repository, IUnitOfWork unitOfWork, IClock clock)
    : ICommandHandler<RenameWidgetCommand>
{
    /// <inheritdoc />
    public async Task<Result> Handle(RenameWidgetCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        var widget = await repository.GetByIdAsync(new WidgetId(command.Id), cancellationToken);
        if (widget is null)
        {
            return Result.Failure(Error.NotFound("widget.not_found", $"Widget '{command.Id}' was not found."));
        }

        widget.Rename(command.Name, clock.UtcNow);
        repository.Update(widget);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}

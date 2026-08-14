using ResQ.BuildingBlocks.Application;
using ResQ.BuildingBlocks.Domain;
using ResQ.Service.Domain;

namespace ResQ.Service.Application;

/// <summary>Handles <see cref="RenameSampleCommand"/>, stamping the timestamp from <see cref="IClock"/>.</summary>
public sealed class RenameSampleHandler(ISampleRepository repository, IUnitOfWork unitOfWork, IClock clock)
    : ICommandHandler<RenameSampleCommand>
{
    /// <inheritdoc />
    public async Task<Result> Handle(RenameSampleCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        var sample = await repository.GetByIdAsync(new SampleId(command.Id), cancellationToken);
        if (sample is null)
        {
            return Result.Failure(Error.NotFound("sample.not_found", $"Sample '{command.Id}' was not found."));
        }

        sample.Rename(command.Name, clock.UtcNow);
        repository.Update(sample);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}

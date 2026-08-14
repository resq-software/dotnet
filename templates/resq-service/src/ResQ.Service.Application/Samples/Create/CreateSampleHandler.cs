using ResQ.BuildingBlocks.Application;
using ResQ.BuildingBlocks.Domain;
using ResQ.Service.Domain;

namespace ResQ.Service.Application;

/// <summary>Handles <see cref="CreateSampleCommand"/>, stamping timestamps from <see cref="IClock"/>.</summary>
public sealed class CreateSampleHandler(ISampleRepository repository, IUnitOfWork unitOfWork, IClock clock)
    : ICommandHandler<CreateSampleCommand, Guid>
{
    /// <inheritdoc />
    public async Task<Result<Guid>> Handle(CreateSampleCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        var sample = Sample.Create(SampleId.New(), command.Name, command.Quantity, clock.UtcNow);
        await repository.AddAsync(sample, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(sample.Id.Value);
    }
}

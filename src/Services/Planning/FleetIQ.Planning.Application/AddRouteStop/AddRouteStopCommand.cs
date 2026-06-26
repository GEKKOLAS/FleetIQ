using FleetIQ.BuildingBlocks.Application;
using FleetIQ.BuildingBlocks.Domain;
using FluentValidation;
using FleetIQ.Planning.Application.Abstractions;
using FleetIQ.Planning.Domain;

namespace FleetIQ.Planning.Application.AddRouteStop;

public sealed record AddRouteStopCommand(Guid RouteId, string Kind, string City, string Address, double Latitude, double Longitude) : ICommand<Result>;

public sealed class AddRouteStopCommandValidator : AbstractValidator<AddRouteStopCommand>
{
    public AddRouteStopCommandValidator()
    {
        RuleFor(x => x.RouteId).NotEmpty();
        RuleFor(x => x.City).NotEmpty();
        RuleFor(x => x.Latitude).InclusiveBetween(-90, 90);
        RuleFor(x => x.Longitude).InclusiveBetween(-180, 180);
        RuleFor(x => x.Kind).Must(k => Enum.TryParse<StopKind>(k, true, out _))
            .WithMessage("Kind must be 'Pickup' or 'Delivery'.");
    }
}

public sealed class AddRouteStopCommandHandler(IRouteRepository repository, IUnitOfWork unitOfWork)
    : ICommandHandler<AddRouteStopCommand, Result>
{
    public async Task<Result> Handle(AddRouteStopCommand command, CancellationToken ct)
    {
        var route = await repository.GetByIdAsync(command.RouteId, ct);
        if (route is null) return Result.Failure(RouteErrors.NotFound);

        var kind = Enum.Parse<StopKind>(command.Kind, true);
        var result = route.AddStop(kind, command.City, command.Address, command.Latitude, command.Longitude);
        if (result.IsFailure) return result;

        await unitOfWork.SaveChangesAsync(ct);
        return Result.Success();
    }
}

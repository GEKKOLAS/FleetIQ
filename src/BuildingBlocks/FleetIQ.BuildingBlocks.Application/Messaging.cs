namespace FleetIQ.BuildingBlocks.Application;

// Lightweight, dependency-free CQRS contracts (no MediatR -> no licensing concern).
// Handlers are registered in DI and injected directly into endpoints.

public interface ICommand<TResponse> { }
public interface IQuery<TResponse> { }

public interface ICommandHandler<in TCommand, TResponse> where TCommand : ICommand<TResponse>
{
    Task<TResponse> Handle(TCommand command, CancellationToken ct);
}

public interface IQueryHandler<in TQuery, TResponse> where TQuery : IQuery<TResponse>
{
    Task<TResponse> Handle(TQuery query, CancellationToken ct);
}

/// <summary>Commit boundary, implemented by each service's DbContext.</summary>
public interface IUnitOfWork
{
    Task<int> SaveChangesAsync(CancellationToken ct = default);
}

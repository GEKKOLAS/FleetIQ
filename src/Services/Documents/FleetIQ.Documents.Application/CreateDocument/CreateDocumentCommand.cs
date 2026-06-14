using FleetIQ.BuildingBlocks.Application;
using FleetIQ.BuildingBlocks.Domain;
using FluentValidation;
using FleetIQ.Documents.Application.Abstractions;
using FleetIQ.Documents.Domain;

namespace FleetIQ.Documents.Application.CreateDocument;

public sealed record CreateDocumentCommand(Guid OwnerId, string Category, string StorageRef) : ICommand<Result<Guid>>;

public sealed class CreateDocumentCommandValidator : AbstractValidator<CreateDocumentCommand>
{
    public CreateDocumentCommandValidator()
    {
        RuleFor(x => x.OwnerId).NotEmpty();
        RuleFor(x => x.Category).NotEmpty();
        RuleFor(x => x.StorageRef).NotEmpty();
    }
}

public sealed class CreateDocumentCommandHandler(IDocumentRepository repository, IUnitOfWork unitOfWork)
    : ICommandHandler<CreateDocumentCommand, Result<Guid>>
{
    public async Task<Result<Guid>> Handle(CreateDocumentCommand command, CancellationToken ct)
    {
        var entity = Document.Create(command.OwnerId, command.Category, command.StorageRef);
        await repository.AddAsync(entity, ct);
        await unitOfWork.SaveChangesAsync(ct);
        return Result.Success(entity.Id);
    }
}

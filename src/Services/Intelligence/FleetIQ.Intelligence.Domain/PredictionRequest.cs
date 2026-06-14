using FleetIQ.BuildingBlocks.Domain;

namespace FleetIQ.Intelligence.Domain;

// NOTE: Thin inference service: call managed AI APIs or ONNX Runtime; defer in-house training. ETA prediction, OCR, anomaly detection.
public enum PredictionRequestStatus { Requested = 0, Completed = 1, Failed = 2 }

public static class PredictionRequestErrors
{
    public static readonly Error InvalidTransition =
        new("PredictionRequest.InvalidTransition", "The item is not in a state that allows this operation.");
}

/// <summary>PredictionRequest aggregate root.</summary>
public sealed class PredictionRequest : AggregateRoot<Guid>
{
    public string ModelId { get; private set; } = string.Empty;
    public string PredictionType { get; private set; } = string.Empty;
    public PredictionRequestStatus Status { get; private set; }
    public DateTime CreatedOnUtc { get; private set; }

    private PredictionRequest() { } // EF

    private PredictionRequest(Guid id, string modelId, string predictionType) : base(id)
    {
        ModelId = modelId;
        PredictionType = predictionType;
        Status = PredictionRequestStatus.Requested;
        CreatedOnUtc = DateTime.UtcNow;
    }

    public static PredictionRequest Create(string modelId, string predictionType)
    {
        var entity = new PredictionRequest(Guid.NewGuid(), modelId, predictionType);
        entity.Raise(new Events.PredictionRequestCreatedDomainEvent(entity.Id));
        return entity;
    }

    public Result Complete()
    {
        if (Status != PredictionRequestStatus.Requested) return Result.Failure(PredictionRequestErrors.InvalidTransition);
        Status = PredictionRequestStatus.Completed;
        Raise(new Events.PredictionRequestStatusChangedDomainEvent(Id, Status.ToString()));
        return Result.Success();
    }
}

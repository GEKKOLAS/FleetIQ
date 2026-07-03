using FleetIQ.BuildingBlocks.Domain;

namespace FleetIQ.Intelligence.Domain;

// NOTE: Thin inference service: call managed AI APIs or ONNX Runtime; defer in-house training. ETA prediction, OCR, anomaly detection.
public enum PredictionRequestStatus { Requested = 0, Running = 1, Completed = 2, Failed = 3 }

public static class PredictionRequestErrors
{
    public static readonly Error InvalidTransition =
        new("PredictionRequest.InvalidTransition", "The item is not in a state that allows this operation.");
    public static readonly Error ModelRequired =
        new("PredictionRequest.ModelRequired", "A model id is required.");
    public static readonly Error TypeInvalid =
        new("PredictionRequest.TypeInvalid", "Prediction type must be one of: Eta, Delay, Price, Anomaly.");
    public static readonly Error ResultRequired =
        new("PredictionRequest.ResultRequired", "A result payload is required to complete a prediction.");
    public static readonly Error ConfidenceOutOfRange =
        new("PredictionRequest.ConfidenceOutOfRange", "Confidence must be between 0 and 1.");
    public static readonly Error ErrorRequired =
        new("PredictionRequest.ErrorRequired", "An error message is required to fail a prediction.");
}

/// <summary>PredictionRequest aggregate root. Lifecycle: Requested -> Running -> Completed | Failed.</summary>
public sealed class PredictionRequest : AggregateRoot<Guid>
{
    private static readonly string[] AllowedTypes = ["Eta", "Delay", "Price", "Anomaly"];

    public string ModelId { get; private set; } = string.Empty;
    public string PredictionType { get; private set; } = string.Empty;
    public PredictionRequestStatus Status { get; private set; }
    public DateTime CreatedOnUtc { get; private set; }
    public DateTime? StartedOnUtc { get; private set; }
    public DateTime? FinishedOnUtc { get; private set; }
    public string? ResultJson { get; private set; }
    public decimal? Confidence { get; private set; }
    public string? Error { get; private set; }

    private PredictionRequest() { } // EF

    private PredictionRequest(Guid id, string modelId, string predictionType) : base(id)
    {
        ModelId = modelId;
        PredictionType = predictionType;
        Status = PredictionRequestStatus.Requested;
        CreatedOnUtc = DateTime.UtcNow;
    }

    public static Result<PredictionRequest> Create(string modelId, string predictionType)
    {
        if (string.IsNullOrWhiteSpace(modelId)) return Result.Failure<PredictionRequest>(PredictionRequestErrors.ModelRequired);
        var normalizedType = AllowedTypes.FirstOrDefault(t => string.Equals(t, predictionType, StringComparison.OrdinalIgnoreCase));
        if (normalizedType is null) return Result.Failure<PredictionRequest>(PredictionRequestErrors.TypeInvalid);

        var entity = new PredictionRequest(Guid.NewGuid(), modelId.Trim(), normalizedType);
        entity.Raise(new Events.PredictionRequestCreatedDomainEvent(entity.Id));
        return Result.Success(entity);
    }

    public Result Start()
    {
        if (Status != PredictionRequestStatus.Requested) return Result.Failure(PredictionRequestErrors.InvalidTransition);

        Status = PredictionRequestStatus.Running;
        StartedOnUtc = DateTime.UtcNow;
        Raise(new Events.PredictionRequestStatusChangedDomainEvent(Id, Status.ToString()));
        return Result.Success();
    }

    public Result Complete(string resultJson, decimal confidence)
    {
        if (Status != PredictionRequestStatus.Running) return Result.Failure(PredictionRequestErrors.InvalidTransition);
        if (string.IsNullOrWhiteSpace(resultJson)) return Result.Failure(PredictionRequestErrors.ResultRequired);
        if (confidence is < 0 or > 1) return Result.Failure(PredictionRequestErrors.ConfidenceOutOfRange);

        Status = PredictionRequestStatus.Completed;
        FinishedOnUtc = DateTime.UtcNow;
        ResultJson = resultJson;
        Confidence = confidence;
        Raise(new Events.PredictionRequestStatusChangedDomainEvent(Id, Status.ToString()));
        return Result.Success();
    }

    public Result Fail(string error)
    {
        if (Status is not (PredictionRequestStatus.Requested or PredictionRequestStatus.Running))
            return Result.Failure(PredictionRequestErrors.InvalidTransition);
        if (string.IsNullOrWhiteSpace(error)) return Result.Failure(PredictionRequestErrors.ErrorRequired);

        Status = PredictionRequestStatus.Failed;
        FinishedOnUtc = DateTime.UtcNow;
        Error = error.Trim();
        Raise(new Events.PredictionRequestStatusChangedDomainEvent(Id, Status.ToString()));
        return Result.Success();
    }
}

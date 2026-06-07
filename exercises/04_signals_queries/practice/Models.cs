// Models.cs — shared types for Exercise 4

namespace VehicleTransaction;

public record VehicleOrder(
    string VehicleId,
    string BuyerId,
    decimal PurchasePrice
);

public record TransactionResult(
    string VehicleId,
    string PaymentConfirmation,
    string Status
);

/// <summary>
/// Snapshot of workflow state returned by the status query.
/// </summary>
public record WorkflowStatus(
    string Stage,
    bool ManagerApprovalRequired,
    bool ManagerApproved
);

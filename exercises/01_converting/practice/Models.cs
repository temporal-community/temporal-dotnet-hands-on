// Models.cs — shared types for the vehicle transaction workflow

namespace VehicleTransaction;

/// <summary>
/// Input passed to the workflow when a vehicle purchase is initiated.
/// </summary>
/// <param name="VehicleId">Unique identifier for the vehicle being sold.</param>
/// <param name="BuyerId">Identifier for the purchasing party.</param>
/// <param name="PurchasePrice">Agreed sale price in USD.</param>
public record VehicleOrder(
    string VehicleId,
    string BuyerId,
    decimal PurchasePrice
);

/// <summary>
/// Result returned when the workflow completes successfully.
/// </summary>
/// <param name="VehicleId">The vehicle that was sold.</param>
/// <param name="PaymentConfirmation">Confirmation token from the payment processor.</param>
/// <param name="Status">Final status string, e.g. "Completed".</param>
public record TransactionResult(
    string VehicleId,
    string PaymentConfirmation,
    string Status
);

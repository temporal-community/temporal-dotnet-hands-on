// Models.cs — shared types for Exercise 3

namespace VehicleTransaction;

public record VehicleOrder(
    string VehicleId,
    string BuyerId,
    decimal PurchasePrice
);

/// <summary>
/// Result of running pre-sale checks in parallel.
/// Both checks must pass before payment proceeds.
/// </summary>
public record PreSaleCheckResult(
    bool FraudCleared,
    bool InventoryConfirmed
);

public record TransactionResult(
    string VehicleId,
    string PaymentConfirmation,
    string Status
);

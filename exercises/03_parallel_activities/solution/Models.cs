// Models.cs — shared types for Exercise 3

namespace VehicleTransaction;

public record VehicleOrder(
    string VehicleId,
    string BuyerId,
    decimal PurchasePrice
);

public record PreSaleCheckResult(
    bool FraudCleared,
    bool InventoryConfirmed
);

public record TransactionResult(
    string VehicleId,
    string PaymentConfirmation,
    string Status
);

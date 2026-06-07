// Models.cs — shared types for Exercise 5

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

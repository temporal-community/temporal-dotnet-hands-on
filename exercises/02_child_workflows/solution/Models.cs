// Models.cs — shared types for the vehicle transaction workflow

namespace VehicleTransaction;

/// <summary>Input passed to the main transaction workflow.</summary>
public record VehicleOrder(
    string VehicleId,
    string BuyerId,
    decimal PurchasePrice
);

/// <summary>Input passed to the document generation child workflow.</summary>
public record DocumentRequest(
    string VehicleId,
    string BuyerId,
    string PaymentConfirmation
);

/// <summary>Result returned by the document generation child workflow.</summary>
public record DocumentResult(
    string BillOfSaleUrl,
    string TitleDocumentUrl
);

/// <summary>Result returned when the top-level workflow completes successfully.</summary>
public record TransactionResult(
    string VehicleId,
    string PaymentConfirmation,
    string BillOfSaleUrl,
    string TitleDocumentUrl,
    string Status
);

// VehicleTransactionActivities.cs — SOLUTION (same as practice)

using Temporalio.Activities;
using Temporalio.Exceptions;

namespace VehicleTransaction;

public class VehicleTransactionActivities
{
    [Activity]
    public async Task<bool> CheckFraudAsync(VehicleOrder order)
    {
        var cleared = await FraudService.CheckAsync(order.BuyerId);
        if (!cleared)
            throw new ApplicationFailureException(
                $"Fraud check failed for buyer {order.BuyerId} — transaction blocked",
                nonRetryable: true);
        return cleared;
    }

    [Activity]
    public async Task<bool> ConfirmInventoryAsync(VehicleOrder order)
    {
        var available = await InventoryService.ConfirmAsync(order.VehicleId);
        if (!available)
            throw new ApplicationFailureException(
                $"Vehicle {order.VehicleId} is no longer available",
                nonRetryable: true);
        return available;
    }

    [Activity]
    public async Task<string> ProcessPaymentAsync(VehicleOrder order)
    {
        return await PaymentService.ChargeAsync(order.BuyerId, order.PurchasePrice);
    }

    [Activity]
    public async Task TransferTitleAsync(VehicleOrder order)
    {
        await TitleService.TransferAsync(order.VehicleId, order.BuyerId);
    }
}

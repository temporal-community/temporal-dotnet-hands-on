// VehicleTransactionActivities.cs — SOLUTION

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

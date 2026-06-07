// VehicleTransactionActivities.cs
//
// Each forward activity now has a corresponding compensation activity.
// Compensations undo the effect of a completed step when a later step fails.
//
// This file is complete — no TODOs here. Read through the compensation
// methods before working on the workflow.

using Temporalio.Activities;
using Temporalio.Exceptions;

namespace VehicleTransaction;

public class VehicleTransactionActivities
{
    // ── Forward activities ───────────────────────────────────────────────────

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

    // ── Compensation activities ──────────────────────────────────────────────
    //
    // These run in reverse order when a later step fails.
    // They should be idempotent — safe to call more than once.

    [Activity]
    public async Task RefundPaymentAsync(VehicleOrder order, string paymentConfirmation)
    {
        await PaymentService.RefundAsync(order.BuyerId, order.PurchasePrice, paymentConfirmation);
        Console.WriteLine($"[Compensation] Refunded payment {paymentConfirmation} for buyer {order.BuyerId}");
    }

    [Activity]
    public async Task RevertTitleTransferAsync(VehicleOrder order)
    {
        await TitleService.RevertAsync(order.VehicleId, order.BuyerId);
        Console.WriteLine($"[Compensation] Reverted title transfer for vehicle {order.VehicleId}");
    }
}

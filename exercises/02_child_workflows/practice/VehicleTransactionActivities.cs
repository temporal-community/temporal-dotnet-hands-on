// VehicleTransactionActivities.cs
//
// Activities are the same three steps from Exercise 1 — fraud check,
// payment, and title transfer — plus two new document generation activities
// that belong to the child workflow.
//
// The [Activity] attribute marks a method as a Temporal activity.

using Temporalio.Activities;
using Temporalio.Exceptions;

namespace VehicleTransaction;

public class VehicleTransactionActivities
{
    // ── Main workflow activities (carried over from Exercise 1) ──────────────

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

    // ── Document generation activities (used by the child workflow) ──────────

    [Activity]
    public async Task<string> GenerateBillOfSaleAsync(DocumentRequest request)
    {
        return await DocumentService.GenerateBillOfSaleAsync(
            request.VehicleId, request.BuyerId, request.PaymentConfirmation);
    }

    [Activity]
    public async Task<string> GenerateTitleDocumentAsync(DocumentRequest request)
    {
        return await DocumentService.GenerateTitleDocumentAsync(
            request.VehicleId, request.BuyerId);
    }
}

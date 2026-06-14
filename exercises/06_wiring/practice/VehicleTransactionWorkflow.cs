// VehicleTransactionWorkflow.cs
//
// This workflow currently calls the downstream services directly — the same
// mistake as the original pipeline. That means no retries, no durability,
// and no separation between orchestration and side effects.
//
// Stage A: pull the service calls out into VehicleTransactionActivities and
//          replace them with Workflow.ExecuteActivityAsync calls. You will
//          also define ActivityOptions with different retry policies for each.
//
// Stage B: add a signal handler and query handler, then add an approval gate
//          for high-value orders (over $50,000).

using Temporalio.Workflows;
using Temporalio.Common;

namespace VehicleTransaction;

[Workflow]
public class VehicleTransactionWorkflow
{
    // TODO (Stage A): Define two ActivityOptions fields.
    //
    // FraudOptions — fraud checks are fast; short timeout, aggressive retry:
    //   StartToCloseTimeout: 10 seconds
    //   RetryPolicy: InitialInterval 1s, MaximumInterval 10s, BackoffCoefficient 2.0
    //
    // PaymentOptions — payment calls can be slow; longer timeout, conservative retry:
    //   StartToCloseTimeout: 60 seconds
    //   RetryPolicy: InitialInterval 2s, MaximumInterval 60s, BackoffCoefficient 2.0,
    //                MaximumAttempts 5
    //
    // Pattern:
    //   private static readonly ActivityOptions FraudOptions = new()
    //   {
    //       StartToCloseTimeout = TimeSpan.FromSeconds(N),
    //       RetryPolicy = new RetryPolicy { ... }
    //   };

    // Stage B state — leave these here, you will need them.
    private string _stage = "starting";
    private bool _managerApprovalRequired = false;
    private bool _managerApproved = false;

    // TODO (Stage B): Add the ApproveTransaction signal handler.
    //
    // A signal handler is a method marked [WorkflowSignal]. It updates
    // workflow state but cannot return a value to the caller.
    //
    // When the signal arrives, set _managerApproved = true.
    //
    // Pattern:
    //   [WorkflowSignal]
    //   public async Task ApproveTransactionAsync()
    //   {
    //       _someField = newValue;
    //   }

    // TODO (Stage B): Add the GetStatus query handler.
    //
    // A query handler is a method marked [WorkflowQuery]. It reads state
    // synchronously — no await, no state mutation.
    //
    // Return a WorkflowStatus with the three state fields.
    //
    // Pattern:
    //   [WorkflowQuery]
    //   public ReturnType GetStatus()
    //   {
    //       return new ReturnType(_field1, _field2, _field3);
    //   }

    [WorkflowRun]
    public async Task<TransactionResult> RunAsync(VehicleOrder order)
    {
        _stage = "fraud-check";

        // TODO (Stage A): Replace this direct service call with an activity.
        //
        // Currently: calls FraudService directly (wrong — non-deterministic,
        //            not retried by Temporal, not visible in event history)
        //
        // Replace with:
        //   await Workflow.ExecuteActivityAsync(
        //       (VehicleTransactionActivities a) => a.CheckFraudAsync(order),
        //       FraudOptions);
        //
        // Use FraudOptions (which you defined above).
        var fraudCleared = await FraudService.CheckAsync(order.BuyerId);
        if (!fraudCleared)
            throw new InvalidOperationException("Fraud check failed");

        // TODO (Stage B): Add approval gate for high-value orders.
        //
        // If order.PurchasePrice > 50_000m:
        //   - Set _managerApprovalRequired = true
        //   - Set _stage = "awaiting-approval"
        //   - Await Workflow.WaitConditionAsync(() => _managerApproved)
        //
        // This pauses the workflow durably until ApproveTransactionAsync
        // delivers the signal. No polling. No timeout. Days if needed.

        _stage = "payment";

        // TODO (Stage A): Replace this direct service call with an activity.
        //
        // Currently: calls PaymentService directly.
        //
        // Replace with:
        //   var paymentConfirmation = await Workflow.ExecuteActivityAsync(
        //       (VehicleTransactionActivities a) => a.ProcessPaymentAsync(order),
        //       PaymentOptions);
        //
        // Use PaymentOptions (which you defined above). Notice this has a longer
        // timeout and fewer maximum attempts than FraudOptions — why?
        var paymentConfirmation = await PaymentService.ChargeAsync(order.BuyerId, order.PurchasePrice);

        _stage = "title-transfer";

        // TODO (Stage A): Replace this direct service call with an activity.
        //
        // Use PaymentOptions for this one too — title transfer is an external
        // call that can fail transiently and benefits from the same conservative policy.
        await TitleService.TransferAsync(order.VehicleId, order.BuyerId);

        _stage = "completed";

        return new TransactionResult(
            VehicleId:           order.VehicleId,
            PaymentConfirmation: paymentConfirmation,
            Status:              "Completed"
        );
    }
}

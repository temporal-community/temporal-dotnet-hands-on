// VehicleTransactionWorkflow.cs
//
// The fraud check and inventory confirmation are independent — neither one
// needs the other's result to start. Running them sequentially wastes time.
//
// Your job: run CheckFraudAsync and ConfirmInventoryAsync in parallel,
// then proceed to payment only when both succeed.

using Temporalio.Workflows;
using Temporalio.Common;

namespace VehicleTransaction;

[Workflow]
public class VehicleTransactionWorkflow
{
    private static readonly ActivityOptions DefaultOptions = new()
    {
        StartToCloseTimeout = TimeSpan.FromSeconds(30),
        RetryPolicy = new RetryPolicy
        {
            InitialInterval    = TimeSpan.FromSeconds(1),
            MaximumInterval    = TimeSpan.FromSeconds(30),
            BackoffCoefficient = 2.0f,
        }
    };

    [WorkflowRun]
    public async Task<TransactionResult> RunAsync(VehicleOrder order)
    {
        // TODO: Run CheckFraudAsync and ConfirmInventoryAsync in parallel
        //
        // Goal: start both activities at the same time and wait for both
        //       to complete before proceeding to payment.
        //
        // In C#, start each activity without awaiting it immediately — this
        // gives you a Task for each. Then await both together.
        //
        // Pattern:
        //   var taskA = Workflow.ExecuteActivityAsync(
        //       (VehicleTransactionActivities a) => a.ActivityAAsync(input),
        //       DefaultOptions);
        //   var taskB = Workflow.ExecuteActivityAsync(
        //       (VehicleTransactionActivities a) => a.ActivityBAsync(input),
        //       DefaultOptions);
        //   await Task.WhenAll(taskA, taskB);
        //
        // Note: inside a Temporal workflow, Task.WhenAll is safe and
        // deterministic. Temporal replays it correctly from history.

        throw new NotImplementedException("TODO: run pre-sale checks in parallel");

        // These two steps are sequential and correct as-is — implement them
        // after the parallel checks above.
        var paymentConfirmation = await Workflow.ExecuteActivityAsync(
            (VehicleTransactionActivities a) => a.ProcessPaymentAsync(order),
            DefaultOptions);

        await Workflow.ExecuteActivityAsync(
            (VehicleTransactionActivities a) => a.TransferTitleAsync(order),
            DefaultOptions);

        return new TransactionResult(
            VehicleId:           order.VehicleId,
            PaymentConfirmation: paymentConfirmation,
            Status:              "Completed"
        );
    }
}

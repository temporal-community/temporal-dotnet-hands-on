// VehicleTransactionWorkflow.cs
//
// The transaction has three steps: fraud check, payment, and title transfer.
// If any step fails after payment has succeeded, we need to undo the
// completed steps in reverse order.
//
// Your job: implement the Saga pattern using a compensation stack.

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
            // Cap attempts so a persistently failing step eventually gives up
            // and triggers compensation, instead of retrying forever.
            MaximumAttempts    = 3,
            InitialInterval    = TimeSpan.FromSeconds(1),
            MaximumInterval    = TimeSpan.FromSeconds(30),
            BackoffCoefficient = 2.0f,
        }
    };

    // Compensation activities should not be retried indefinitely —
    // if a refund fails we want to know quickly, not retry for hours.
    private static readonly ActivityOptions CompensationOptions = new()
    {
        StartToCloseTimeout = TimeSpan.FromSeconds(30),
        RetryPolicy = new RetryPolicy
        {
            MaximumAttempts = 3,
            InitialInterval    = TimeSpan.FromSeconds(1),
            MaximumInterval    = TimeSpan.FromSeconds(10),
            BackoffCoefficient = 2.0f,
        }
    };

    [WorkflowRun]
    public async Task<TransactionResult> RunAsync(VehicleOrder order)
    {
        // The compensation stack holds actions to run in reverse if something
        // fails. Push a compensation onto the stack after each successful
        // forward step that has side effects worth undoing.
        //
        // TODO: Build the Saga
        //
        // The pattern:
        //   var compensations = new Stack<Func<Task>>();
        //   try
        //   {
        //       // Step 1
        //       var result1 = await Workflow.ExecuteActivityAsync(...);
        //       compensations.Push(() => Workflow.ExecuteActivityAsync(
        //           (VehicleTransactionActivities a) => a.CompensateStep1Async(...),
        //           CompensationOptions));
        //
        //       // Step 2 — if this throws, Step 1's compensation runs
        //       await Workflow.ExecuteActivityAsync(...);
        //       compensations.Push(() => Workflow.ExecuteActivityAsync(
        //           (VehicleTransactionActivities a) => a.CompensateStep2Async(...),
        //           CompensationOptions));
        //
        //       // Step 3 — if this throws, Steps 2 and 1 compensate in order
        //       await Workflow.ExecuteActivityAsync(...);
        //   }
        //   catch (Exception)
        //   {
        //       // Run compensations in reverse (stack order)
        //       foreach (var compensate in compensations)
        //           await compensate();
        //       throw;
        //   }
        //
        // Steps to implement (in order):
        //   1. CheckFraudAsync       — no compensation needed (read-only)
        //   2. ProcessPaymentAsync   — compensate with RefundPaymentAsync
        //   3. TransferTitleAsync    — compensate with RevertTitleTransferAsync
        //
        // After all steps succeed, return a TransactionResult with Status = "Completed".

        throw new NotImplementedException("TODO: implement Saga pattern");
    }
}

// VehicleTransactionWorkflow.cs
//
// The workflow is the durable orchestrator — it sequences your activities,
// owns retry configuration, and survives any process crash.
//
// Your job: implement RunAsync so it calls the three activities in order
// and returns a TransactionResult.

using Temporalio.Workflows;
using Temporalio.Common;

namespace VehicleTransaction;

[Workflow]
public class VehicleTransactionWorkflow
{
    // TODO: Define ActivityOptions
    //
    // Goal: create a static ActivityOptions field that sets:
    //   - StartToCloseTimeout: how long a single activity attempt may run
    //     before Temporal considers it timed out. Use 30 seconds.
    //   - RetryPolicy: configure InitialInterval (1s), MaximumInterval (30s),
    //     and BackoffCoefficient (2.0). Leave MaximumAttempts at default
    //     (unlimited) — let Temporal keep retrying transient failures.
    //
    // Pattern:
    //   private static readonly ActivityOptions DefaultOptions = new()
    //   {
    //       StartToCloseTimeout = TimeSpan.FromSeconds(N),
    //       RetryPolicy = new RetryPolicy
    //       {
    //           InitialInterval    = TimeSpan.FromSeconds(N),
    //           MaximumInterval    = TimeSpan.FromSeconds(N),
    //           BackoffCoefficient = N,
    //       }
    //   };

    // TODO: Implement RunAsync
    //
    // Goal: call all three activities in sequence using Workflow.ExecuteActivityAsync,
    //       then return a TransactionResult with Status = "Completed".
    //
    // Pattern:
    //   var result = await Workflow.ExecuteActivityAsync(
    //       (YourActivitiesClass a) => a.YourMethodAsync(input),
    //       DefaultOptions);
    //
    // Workflow.ExecuteActivityAsync is how workflows call activities. The lambda
    // tells Temporal which activity method to schedule; it never executes locally.
    // Pass DefaultOptions (defined above) as the second argument.
    [WorkflowRun]
    public async Task<TransactionResult> RunAsync(VehicleOrder order)
    {
        throw new NotImplementedException("TODO: implement RunAsync");
    }
}

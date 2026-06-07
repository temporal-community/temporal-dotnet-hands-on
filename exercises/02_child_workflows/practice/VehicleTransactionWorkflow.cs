// VehicleTransactionWorkflow.cs
//
// The main workflow. It runs the three transaction steps from Exercise 1,
// then hands off to a child workflow for document generation.
//
// Your job: implement the child workflow call in RunAsync.

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
        // These three steps are complete — carried over from Exercise 1.
        await Workflow.ExecuteActivityAsync(
            (VehicleTransactionActivities a) => a.CheckFraudAsync(order),
            DefaultOptions);

        var paymentConfirmation = await Workflow.ExecuteActivityAsync(
            (VehicleTransactionActivities a) => a.ProcessPaymentAsync(order),
            DefaultOptions);

        await Workflow.ExecuteActivityAsync(
            (VehicleTransactionActivities a) => a.TransferTitleAsync(order),
            DefaultOptions);

        // TODO: Call the DocumentGenerationWorkflow as a child workflow
        //
        // Goal: execute DocumentGenerationWorkflow as a child of this workflow,
        //       passing a DocumentRequest built from order and paymentConfirmation.
        //       Capture the DocumentResult it returns.
        //
        // Child workflows use Workflow.ExecuteChildWorkflowAsync instead of
        // ExecuteActivityAsync. They get their own event history in the UI —
        // look for it as a separate workflow entry after you run this.
        //
        // Pattern:
        //   var result = await Workflow.ExecuteChildWorkflowAsync(
        //       (ChildWorkflowClass wf) => wf.RunAsync(input),
        //       new ChildWorkflowOptions { Id = "some-unique-id" });
        //
        // Use $"docs-{order.VehicleId}" as the child workflow ID.

        throw new NotImplementedException("TODO: call DocumentGenerationWorkflow as a child workflow");
    }
}
